using DotPulsar;
using DotPulsar.Extensions;
using DotPulsar.Abstractions;
using System;
using System.Threading;
using System.Text;
using System.Buffers;
using System.Threading.Tasks;

namespace SuperMQ.Pulsar
{
    /// <summary>
    /// 消费
    /// </summary>
    public class PulsarConsumer : PulsarBase
    {
        public EventHandler<byte[]> Received;
        public EventHandler<ConsumerState> StateChanged;
        public PulsarConsumer(string strPulAddr, string strConsumerTopic, string subscription)
        {
            PubAddr = strPulAddr;
            ProductTopic = strConsumerTopic;
            Subscription = subscription;
        }

        IConsumer<string> consumer = null;
        /// <summary>
        /// 创建消费
        /// </summary>
        /// <param name="strPulAddr">Pulsar地址</param>
        /// <param name="strConsumerTopic">主题：persistent://public/default/mytopic</param>
        public override Task<bool> ConnectAsync()
        {
            try
            {
                client = PulsarClient.Builder()
                    .ServiceUrl(new Uri("pulsar://" + PubAddr))
                    .RetryInterval(new TimeSpan(3))
                    .Build();

                consumer = client.NewConsumer(Schema.String)
                    .StateChangedHandler(MonitorState)
                    .SubscriptionName(Subscription)
                    .Topic(ProductTopic)
                    .Create();
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Task.Run(() => { Error?.Invoke(this, ex); });
                return Task.FromResult(false);
            }
        }

        public override async Task<bool> DisConnectAsync()
        {
            try
            {
                if (consumer != null)
                {
                    await consumer.Unsubscribe();
                    await consumer.DisposeAsync();
                    consumer = null;
                }
                if (client != null)
                {
                    await client.DisposeAsync();
                    client = null;
                }
                return true;
            }
            catch (Exception ex)
            {
                _ = Task.Run(() => { Error?.Invoke(this, ex); });
                return false;
            }
        }

        /// <summary>
        /// 开始接收消息
        /// </summary>
        public void ReceiveStart()
        {
            if (IsConnected)
            {
                tokenSource = new CancellationTokenSource();
                cancellationToken = tokenSource.Token;
                _ = Task.Run(async () =>
                {
                    IsReceive = true;
                    //3.消费消息
                    await foreach (var message in consumer?.Messages(cancellationToken))
                    {
                        Received?.Invoke(this, message.Data.ToArray());
                        Console.WriteLine("Received: " + Encoding.UTF8.GetString(message.Data.ToArray()));
                        await consumer.Acknowledge(message);
                    }
                });
            }
        }
        /// <summary>
        /// 取消接收消息
        /// </summary>
        public void ReceiveStop()
        {
            if (IsReceive)
            {
                if (cancellationToken.CanBeCanceled)
                {
                    tokenSource?.Cancel();
                    IsReceive = false;
                }
            }
        }
        /// <summary>
        /// 监测消费者状态
        /// </summary>
        /// <param name="stateChanged"></param>
        /// <param name="cancellationToken"></param>
        private async ValueTask MonitorState(ConsumerStateChanged stateChanged, CancellationToken cancellationToken)
        {
            var state = ConsumerState.Disconnected;
            var topic = stateChanged.Consumer.Topic;
            //if (!cancellationToken.IsCancellationRequested)
            //{
            state = (await consumer.StateChangedFrom(state, cancellationToken)).ConsumerState;
            if (state is ConsumerState.Active)
                IsConnected = true;
            else if (state is ConsumerState.Disconnected)
                IsConnected = false;
            _ = Task.Run(() =>
            {
                StateChanged.Invoke(this, state);
            });
            //string stateMessage;
            var stateMessage = state switch
            {
                ConsumerState.Active => "The consumer is active",
                ConsumerState.Inactive => "The consumer is inactive",
                ConsumerState.Disconnected => "The consumer is disconnected",
                ConsumerState.Closed => "The consumer has closed",
                ConsumerState.ReachedEndOfTopic => "The consumer has reached end of topic",
                ConsumerState.Faulted => "The consumer has faulted",
                ConsumerState.Unsubscribed => "The consumer is unsubscribed.",
                _ => $"The consumer has an unknown state '{state}'"
            };
            //switch (state)
            //{
            //    case ConsumerState.Active:
            //        stateMessage = "The consumer is active";
            //        break;
            //    case ConsumerState.Closed:
            //        stateMessage = "The consumer has closed";
            //        break;
            //    case ConsumerState.Disconnected:
            //        stateMessage = "The consumer is disconnected";
            //        break;
            //    case ConsumerState.Faulted:
            //        stateMessage = "The consumer has faulted";
            //        break;
            //    case ConsumerState.Inactive:
            //        stateMessage = "The consumer is inactive";
            //        break;
            //    case ConsumerState.ReachedEndOfTopic:
            //        stateMessage = "The consumer has reached end of topic";
            //        break;
            //    case ConsumerState.Unsubscribed:
            //        stateMessage = "The consumer is unsubscribed.";
            //        break;
            //    default:
            //        stateMessage = $"The consumer has an unknown state '{state}'";
            //        break;
            //}
            Console.WriteLine($"The consumer for topic '{topic}' " + stateMessage);
            if (consumer.IsFinalState(state))
                return;

            //}
        }

    }
}
