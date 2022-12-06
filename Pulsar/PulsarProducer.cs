using DotPulsar;
using DotPulsar.Extensions;
using System;
using System.Threading;
using DotPulsar.Internal;
using System.Threading.Tasks;

namespace SuperMQ.Pulsar
{
    /// <summary>
    /// 生产
    /// </summary>
    public class PulsarProducer : PulsarBase
    {
        public event EventHandler<ProducerState> StateChanged;
        Producer<string> producer = null;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="strPulAddr">Pulsar地址</param>
        /// <param name="strProduceTopic">主题：persistent://public/default/mytopic</param>
        public PulsarProducer(string strPulAddr, string strProduceTopic)
        {
            PubAddr = strPulAddr;
            ProductTopic = strProduceTopic;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strPulAddr">Pulsar地址</param>
        /// <param name="strProduceTopic">主题：persistent://public/default/mytopic</param>
        public override Task<bool> ConnectAsync()
        {
            try
            {
                client = (PulsarClient)PulsarClient.Builder()
                    .ServiceUrl(new Uri("pulsar://" + PubAddr))
                    .ExceptionHandler(ExceptionHandler)
                    .RetryInterval(new TimeSpan(3))
                    .Build();

                producer = (Producer<string>)client.NewProducer(Schema.String)
                    .StateChangedHandler(MonitorState)
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

        private void ExceptionHandler(ExceptionContext obj)
        {
            Error?.Invoke(this,obj.Exception);
        }

        public override async Task<bool> DisConnectAsync()
        {
            try
            {
                if (producer != null)
                {
                    await producer.DisposeAsync();
                    producer = null;
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
        /// 监测生产者状态
        /// </summary>
        /// <param name="stateChanged"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async ValueTask MonitorState(ProducerStateChanged stateChanged, CancellationToken cancellationToken)
        {
            try
            {
                var state = ProducerState.Disconnected;
                var topic = stateChanged.Producer.Topic;
                //while(!cancellationToken.IsCancellationRequested)//监测生产者状态
                //if (!cancellationToken.IsCancellationRequested)
                //{
                state = (await producer.StateChangedFrom(state, cancellationToken)).ProducerState;
                IsConnected = stateChanged.ProducerState == ProducerState.Connected;
                _ = Task.Run(() =>
                {
                    StateChanged?.Invoke(this, state);
                });
                //string stateMessage;
                var stateMessage = state switch
                {
                    ProducerState.Connected => $"The producer is connected",
                    ProducerState.Disconnected => $"The producer is disconnected",
                    ProducerState.Closed => $"The producer has closed",
                    ProducerState.Faulted => $"The producer has faulted",
                    ProducerState.PartiallyConnected => $"The producer is partially connected.",
                    _ => $"The producer has an unknown state '{state}'"
                };

                Console.WriteLine($"The producer for topic '{topic}' " + stateMessage);
                Console.WriteLine(stateChanged.ProducerState.ToString());

                if (producer.IsFinalState(state))
                    return;

                //}
            }
            catch { }
            
        }

        /// <summary>
        /// 生产数据
        /// </summary>
        /// <param name="msg"></param>
        public void Send(string msg)
        {
            //Console.WriteLine("pulsar发送生产消息:" + msg);
            try
            {
                if (IsConnected)
                {
                    producer?.Send(msg, CancellationToken.None).ConfigureAwait(false);
                    Console.WriteLine($"pulsar发送生产消息完成!");
                }
                else
                {
                    Console.WriteLine($"pulsar发送生产消息失败...");
                }
            }
            catch (Exception ex)
            { Console.WriteLine($"pulsar发送生产消息异常：{ex.Message}"); }
        }


    }
}