using NewLife.Log;
using NewLife.RocketMQ;
using NewLife.RocketMQ.Client;

namespace SuperMQ
{
    public class RocketMQHelper
    {
        public Producer Producer { get; set; }
        public Consumer Consumer { get; set; }
        /// <summary>
        /// 更新或创建主题。重复执行时为更新
        /// </summary>
        /// <param name="topic">主题</param>
        /// <param name="queueNum">队列数</param>
        public static void CreateOrUpdateTopic(string server, string topic, bool isConsumer, int queueNum = 16)
        {
            MqBase mq;
            if (!isConsumer)
                mq = new Producer();
            else
                mq = new Consumer();

            //Topic = "nx_test",
            mq.NameServerAddress = server;
            mq.Log = XTrace.Log;

            mq.Start();
            // 创建topic时，start前不能指定topic，让其使用默认TBW102
            //Assert.Equal("TBW102", mq.Topic);

            mq.CreateTopic(topic, queueNum);
            mq.Stop();
        }
        //public RocketMQParameter Parameter { get; }

        //public RocketMQConsumerHelper(RocketMQParameter parameter)
        //{
        //    Parameter = parameter;
        //    producer = new Producer()
        //    {
        //        Topic = Parameter.Topics[0],
        //        NameServerAddress = Parameter.ServerUrl,
        //        Log = XTrace.Log,
        //    };
        //    producer.Configure(MqSetting.Current);
        //    producer.OnDisposed += Producer_OnDisposed;
        //    //consumer = new NewLife.RocketMQ.Consumer
        //    //{
        //    //    Topic = Parameter.Topics[0],
        //    //    Group = Parameter.Group,
        //    //    NameServerAddress = Parameter.ServerUrl,
        //    //    //设置每次接收消息只拉取一条信息
        //    //    BatchSize = 1,
        //    //    //FromLastOffset = true,
        //    //    //SkipOverStoredMsgCount = 0,
        //    //    //BatchSize = 20,
        //    //    //Log = NewLife.Log.XTrace.Log,
        //    //};
        //    //consumer.OnConsume = (q, ms) =>
        //    //{
        //    //    string mInfo = $"BrokerName={q.BrokerName},QueueId={q.QueueId},Length={ms.Length}";
        //    //    Console.WriteLine(mInfo);
        //    //    foreach (var item in ms.ToList())
        //    //    {
        //    //        string msg = $"消息：msgId={item.MsgId},key={item.Keys}，产生时间【{item.BornTimestamp.ToDateTime()}】，内容>{item.Body.ToStr()}";
        //    //        Console.WriteLine(msg);
        //    //    }
        //    //    //   return false;//通知消息队：不消费消息
        //    //    return true;		//通知消息队：消费了消息
        //    //};
        //}

        //private void Producer_OnDisposed(object sender, EventArgs e)
        //{

        //}

        //public void Dispose()
        //{
        //    producer?.Dispose();
        //}

        ////public Task<bool> Publish(List<string> topics, string message)
        ////{
        ////    throw new NotImplementedException();
        ////}

        ////public Task<bool> Publish(string message)
        ////{
        ////    throw new NotImplementedException();
        ////}

        ////public Task<bool> Publish(string topic, string message)
        ////{
        ////    producer.Publish(topic, message);
        ////}
        //public async Task<bool> PublishAsync(object body, string tags, string keys=null)
        //{
        //   SendResult sendResult= await producer.PublishAsync(body, tags, keys);
        //    return sendResult?.Status == SendStatus.SendOK;
        //}
        //public void Start()
        //{

        //    producer?.Start();
        //}

        //public void Stop()
        //{
        //    throw new NotImplementedException();
        //}

        ///// <summary>
        ///// 参数
        ///// </summary>
        //public class RocketMQParameter : MQFactory.MQParameter
        //{
        //    public MQType RocketType { get; set; }
        //    /// <summary>
        //    /// 服务质量
        //    /// <para>0 - 至多一次</para>
        //    /// <para>1 - 至少一次</para>
        //    /// <para>2 - 刚好一次</para>
        //    /// </summary>
        //    public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; } = 0;
        //    public string Group { get; set; }
        //    /// <summary>
        //    /// 消费力 每次消费消息数量
        //    /// </summary>
        //    public int BatchSize { get; set; }
        //    /// <summary>
        //    /// 消息体 
        //    /// </summary>
        //    public class MessageInfo
        //    {
        //        public string Text { get; set; }
        //        public string Topic { get; set; }
        //        public string QoS { get; set; }
        //        public string Retained { get; set; }
        //    }
        //    public enum MQType
        //    {
        //        MQTT,
        //        WebSocket,
        //        Tcp
        //    }
        //}
    }
}
