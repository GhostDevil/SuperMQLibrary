using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using static SuperMQ.ActiveMqHelper.MQConfig;

namespace SuperMQ
{
    /// <summary>
    /// <para>日 期:2022-06-23</para>
    /// <para>作 者:不良帥</para>
    /// <para>描 述:使用Apache-ActiveMQ实现消息传送服,Consumer集群模式</para>
    /// </summary>
    public class ActiveMqHelper : MQFactory.IMQClient
    {
        #region 委托事件
        /// <summary>
        /// 接受数据委托
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageType"></param>
        public delegate void Message(object message, MessageType messageType);
        /// <summary>
        /// 接受数据事件
        /// </summary>
        public event Message OnRevMessage;
        /// <summary>
        /// 接受数据委托
        /// </summary>
        /// <param name="message">mq消息对象</param>
        public delegate void MessageEnhance(MqMessage message);
        /// <summary>
        /// 接受数据事件
        /// </summary>
        public event MessageEnhance OnRevMessageEnhance;
        /// <summary>
        ///  在传输过程中发生了不可恢复的异常委托
        /// </summary>
        /// <param name="exception">异常消息</param>
        public delegate void ConnectionExceptionListener(Exception exception);
        /// <summary>
        ///  在传输过程中发生了不可恢复的异常事件
        /// </summary>
        public event ConnectionExceptionListener OnConnectionExceptionListener;
        /// <summary>
        ///  传输状态委托
        /// </summary>
        /// <param name="state">连接状态</param>
        public delegate void ConnectionListener(ConnectState state);
        /// <summary>
        /// 传输状态事件
        /// </summary>
        public event ConnectionListener OnConnectionListener;
        /// <summary>
        ///  会话状态委托
        /// </summary>
        /// <param name="state">会话状态</param>
        public delegate void SessionListener(SessionState state);
        /// <summary>
        /// 会话状态事件
        /// </summary>
        public event SessionListener OnSessionListener;

        #endregion

        #region 枚举
        /// <summary>
        /// 连接状态
        /// </summary>
        public enum ConnectState
        {
            /// <summary>
            /// 传输中断后，连接恢复
            /// </summary>
            ResumedListener,
            /// <summary>
            /// 传输受到了干扰，希望从这一插曲中恢复过来。(开始重连)
            /// </summary>
            InterruptedListener

        }
        /// <summary>
        /// 会话状态
        /// </summary>
        public enum SessionState
        {
            /// <summary>
            /// 开始
            /// </summary>
            Started,
            /// <summary>
            /// 提交
            /// </summary>
            Committed,
            /// <summary>
            /// 撤回
            /// </summary>
            RolledBack

        }

        #endregion

        #region  内部对象 

        /// <summary>
        /// 配置对象
        /// </summary>
        private static MQConfig Config = null;
        /// <summary>
        /// 是否初始化
        /// </summary>
        private static bool isInit = false;

        /// <summary>
        /// 连接工厂
        /// </summary>
        private static IConnectionFactory connectionFactory;
        /// <summary>
        /// 连接对象
        /// </summary>
        private static IConnection connection = null;
        /// <summary>
        /// 会话对象
        /// </summary>
        private static ISession session = null;
        ///// <summary>
        ///// 发布/订阅模式
        ///// </summary>
        //private static IDestination receivDestination = null;
        ///// <summary>
        ///// 发布/订阅模式
        ///// </summary>
        //private static IDestination sendDestination = null;
        ///// <summary>
        ///// 发布者
        ///// </summary>
        //private static IMessageProducer producer = null;
        /// <summary>
        /// 消费者集合
        /// </summary>
        private static List<IMessageConsumer> consumers = null;
        //bool durable = false;


        #endregion

        /// <summary>
        /// 初始化MQ
        /// </summary>
        /// <param name="config">mq配置对象</param>
        /// <param name="isListener">是否使用监听接受消息</param>
        /// <returns>成功返回true，否则失败</returns>
        public ActiveMqHelper(MQConfig config, bool isListener = true)
        {
            try
            {
                consumers = new List<IMessageConsumer>();
                //durable = config.IsTopicDurable;
                Config = config;

                #region 参数说明
                //initialReconnectDelay：默认为10，单位毫秒，表示第一次尝试重连之前等待的时间。

                //maxReconnectDelay：默认30000，单位毫秒，表示两次重连之间的最大时间间隔。

                //useExponentialBackOff：默认为true，表示重连时是否加入避让指数来避免高并发。

                //reconnectDelayExponent：默认为2.0，重连时使用的避让指数。

                //maxReconnectAttempts：5.6版本之前默认为 - 1,5.6版本及其以后，默认为0,0表示重连的次数无限，配置大于0可以指定最大重连次数。

                //startupMaxReconnectAttempts：默认为0，如果该值不为0，表示客户端接收到消息服务器发送来的错误消息之前尝试连接服务器的最大次数，一旦成功连接后，maxReconnectAttempts值开始生效，如果该值为0，则默认采用maxReconnectAttempts。 

                //randomize：默认为true，表示在URI列表中选择URI连接时是否采用随机策略，记住，这种随机策略在第一次选择URI列表中的地址时就开始生效，所以，如果为true的话，一个生产者和一个消费者的Failover连接地址都是两个URI的话，有可能生产者连接的是第一个，而消费者连接的是第二个，造成一个服务器上只有生产者，一个服务器上只有消费者的尴尬境地。

                //backup：默认为false，表示是否在连接初始化时将URI列表中的所有地址都初始化连接，以便快速的失效转移，默认是不开启。

                //timeout：默认为 - 1，单位毫秒，是否允许在重连过程中设置超时时间来中断的正在阻塞的发送操作。-1表示不允许，其他表示超时时间。

                //trackMessages：默认值为false，是否缓存在发送中（in-flight messages）的消息，以便重连时让新的Transport继续发送。默认是不开启。

                //maxCacheSize：默认131072，如果trackMessages为true，该值表示缓存消息的最大尺寸，单位byte。

                //updateURIsSupported：默认值为true，表示重连时客户端新的连接器（Transport）是否从消息服务接受接受原来的URI列表的更新，5.4及其以后的版本可用。如果关闭的话，会导致重连后连接器没有其他的URI地址可以Failover。

                //updateURIsURL：默认为null，从5.4及其以后的版本，ActiveMQ支持从文件中加载Failover的URI地址列表，URI还是以逗号分隔，updateURIsURL为文件路径。

                //nested.*：默认为null，5.9及其以后版本可用，表示给嵌套的URL添加额外的选项。 以前，如果你想检测让死连接速度更快，你必须在wireFormat.maxInactivityDuration = 1000选项添加到失效转移列表中的所有嵌套的URL。例如：
                //failover: (tcp://host01:61616?wireFormat.maxInactivityDuration=1000,tcp://host02:61616?wireFormat.maxInactivityDuration=1000,tcp://host03:61616?wireFormat.maxInactivityDuration=1000) 
                // 而现在，你只需要这样：
                //failover: (tcp://host01:61616,tcp://host02:61616,tcp://host03:61616)?nested.wireFormat.maxInactivityDuration=1000

                //warnAfterReconnectAttempts.*：默认为10，5.10及其以后的版本可用，表示每次重连该次数后会打印日志告警，设置 <= 0的值表示禁用

                //reconnectSupported：默认为true，表示客户端是否应响应经纪人 ConnectionControl事件与重新连接（参见：rebalanceClusterClients）。

                //如果你使用Failover失效转移，则消息服务器在死亡的那一刻，你的生产者发送消息时默认将阻塞，但你可以设置发送消息阻塞的超时时间 failover:(tcp://primary:61616)?timeout=3000
                //如果用户希望能追踪到重连过程，可以在ActiveMQConnectionFactory设置一个TransportListener

                // Example connection strings:
                //    activemq:tcp://activemqhost:61616
                //    stomp:tcp://activemqhost:61613
                //    ems:tcp://tibcohost:7222
                //    msmq://localhost
                #endregion

                //创建连接工厂

                connectionFactory = new ConnectionFactory(string.Format(
                    "failover:(tcp://{0}:{1})?nested." +
                    "wireFormat.maxInactivityDuration=0" +
                    "&maxInactivityDurationInitalDelay=3000" +
                    "&maxReconnectDelay={2}" +
                    "&connection.AsyncSend={3}", config.ServerUrl, config.Port, config.ReConnectionTike, config.UseAsyncSend));

                //通过工厂构建连接,如果你是缺省方式启动Active MQ服务，则不需填用户名、密码 .CreateConnection(userid, pwd);
                connection = config.User != "" && config.Password != "" ? connectionFactory.CreateConnection(config.User, config.Password) : connectionFactory.CreateConnection();

                connection.ExceptionListener += (e) => OnConnectionExceptionListener?.Invoke(e);
                connection.ConnectionInterruptedListener += () => OnConnectionListener?.Invoke(ConnectState.InterruptedListener);
                connection.ConnectionResumedListener += () => OnConnectionListener?.Invoke(ConnectState.ResumedListener);

                //连接的客户端名称标识,如果你要持久“订阅”，则需要设置ClientId，这样程序运行当中被停止，恢复运行时，能拿到没接收到的消息！   
                connection.ClientId = config.SendId; //Guid.NewGuid().ToString();
                //创建Session会话
                session = connection.CreateSession();
                session.TransactionCommittedListener += (e) => OnSessionListener?.Invoke(SessionState.Committed);
                session.TransactionRolledBackListener += (e) => OnSessionListener?.Invoke(SessionState.RolledBack);
                session.TransactionStartedListener += (e) => OnSessionListener?.Invoke(SessionState.Started);

                //新建消费者对象:普通“订阅”模式


                //新建消费者对象:持久"订阅"模式： 持久“订阅”后，如果你的程序被停止工作后，恢复运行，从第一次持久订阅开始，没收到的消息还可以继续收

                if (config.Topics?.Count > 0)
                {
                    for (int i = 0; i < config.Topics.Count; i++)
                    {
                        if (config.IsTopic)
                            consumers.Add(session.CreateDurableConsumer(session.GetTopic(config.Topics[i]), config.ReceiveId, null, false));
                        else
                        {
                            IDestination sendDestination = config.TypeEnum switch
                            {
                                MqMessageTypeEnum.Queue => new ActiveMQQueue(config.Topics[i]),
                                _ => new ActiveMQTopic(config.Topics[i]),
                            };
                            consumers.Add(session.CreateConsumer(session.GetTopic(config.Topics[i])));
                        }
                    }
                }

                if (isListener)//是否启用消息接收
                {
                    //注册监听事件
                    consumers.ForEach(o => { o.Listener += new MessageListener(Consumer_Listener); });

                }


            }
            catch (Exception)
            {

                throw;
            }
        }
        #region 事件

        void Consumer_Listener(IMessage message)
        {
            try
            {
                //IBytesMessage msgByte=null;
                //ITextMessage msg = null;
                MessageType type = MessageType.ITextMessage;
                switch (message)
                {
                    case IBytesMessage messageBytes:
                        {
                            type = MessageType.IBytesMessage;
                            byte[] bytes = new byte[(int)messageBytes.BodyLength]; //得到byte[]数据流
                            messageBytes.ReadBytes(bytes); //读取byte                    
                            OnRevMessage?.Invoke(bytes, MessageType.IBytesMessage);
                            break;
                        }
                    case ITextMessage messageText:
                        type = MessageType.ITextMessage;
                        OnRevMessage?.Invoke(messageText.Text, MessageType.ITextMessage);
                        break;
                    case IMapMessage messageMap:
                        type = MessageType.IMapMessage;
                        OnRevMessage?.Invoke(messageMap, MessageType.IMapMessage);
                        break;
                    case IObjectMessage messageObject:
                        type = MessageType.IObjectMessage;
                        OnRevMessage?.Invoke(messageObject, MessageType.IObjectMessage);
                        break;
                    case IStreamMessage messageStream:
                        type = MessageType.IStreamMessage;
                        OnRevMessage?.Invoke(messageStream, MessageType.IStreamMessage);
                        break;
                }

                OnRevMessageEnhance?.Invoke(new MqMessage()
                {
                    Content = message,
                    CorrelationID = message.NMSCorrelationID,
                    DeliveryMode = message.NMSDeliveryMode.ToString(),
                    MessageId = message.NMSMessageId,
                    Redelivered = message.NMSRedelivered,
                    Timestamp = message.NMSTimestamp,
                    TimeToLive = message.NMSTimeToLive,
                    Type = message.NMSType,
                    ContentType = type
                });
            }
            catch { }
        }
        #endregion


        public async void Start()
        {
            //启动连接，监听要主动启动连接
            await connection.StartAsync();
            isInit = true;
        }

        public async void Stop()
        {
            if (connection != null)
            {
                try
                {
                    isInit = false;
                    foreach (var item in consumers)
                    {
                        item.Listener -= Consumer_Listener;
                        await item.CloseAsync();
                        item.Dispose();
                    }
                    await connection.StopAsync();
                    await connection.CloseAsync();
                }
                catch (Exception) { }
            }
        }

        public void Dispose()
        {
            session?.Dispose();
            session = null;
            connection?.Dispose();
            connectionFactory = null;

        }

        public async Task<bool> Publish(List<string> topics, string message)
        {
            await Task.Run(() =>
            {
                topics?.ForEach(async topic => { await Publish(topic, message); });
            });
            return true;
        }

        public async Task<bool> Publish(string message)
        {
            SendMessage(message, Config.Topics);
            return await Task.FromResult(true);
        }

        public async Task<bool> Publish(string topic, string message)
        {
            SendMessage(message, new List<string>() { topic });
            return await Task.FromResult(true);
        }
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="msgTxt">字符串</param>
        /// <param name="msgDeliveryMode">这是传输模式,Q服务器停止工作后，消息是否保留</param>
        /// <param name="isPersistent">是否持久订阅，持久消息/非持久消息,只是影响宕机后。消息是否会丢失,如果永远不会宕机,那么持久消息和非持久消息没有区别。</param>
        /// <param name="msgKey">实时消息类型，缺省则使用全局消息类型</param>
        /// <param name="msgValue">实时消息类型值，缺省则使用全局消息类型值</param>
        /// <remarks> 持久订阅者/非持久订阅者,只影响离线的时候消息(包括持久消息和非持久消息)是否能接收到,和消息是否持久无关; 持久消息/非持久消息,只是影响宕机后。消息是否会丢失,如果永远不会宕机,那么持久消息和非持久消息没有区别。 </remarks>
        /// <returns>成功返回true，失败返回false</returns>
        static bool SendMessage(string msgTxt, List<string> Topics, bool isPersistent = false, string msgKey = "", string msgValue = "")
        {
            try
            {

                if (!isInit)
                    return false;
                //DeliveryMode.PERSISTENT 当activemq关闭的时候，队列数据将会被保存
                //DeliveryMode.NON_PERSISTENT 当activemq关闭的时候，队列里面的数据将会被清空
                MsgDeliveryMode msgDeliveryMode = MsgDeliveryMode.NonPersistent;//ActiveMQ服务器停止工作后，消息不再保留
                if (isPersistent)
                    msgDeliveryMode = MsgDeliveryMode.Persistent;
                //通过会话创建生产者，方法里面new出来的是MQ中的Queue
                Topics?.ForEach(async c =>
                {
                    await Task.Run(async() =>
                    {
                        using IMessageProducer prod = session.CreateProducer(session.GetTopic(c));
                        prod.DeliveryMode = msgDeliveryMode;
                        //创建一个发送的消息对象
                        ITextMessage message = prod.CreateTextMessage();
                        //给这个对象赋实际的消息
                        message.Text = msgTxt;
                        //设置消息对象的属性，这个很重要哦，是Queue的过滤条件，也是P2P消息的唯一指定属性
                        if (string.IsNullOrWhiteSpace(msgKey) || string.IsNullOrWhiteSpace(msgValue))
                            message.Properties.SetString(Config.MsgKey, Config.MsgValue);
                        else
                            message.Properties.SetString(msgKey, msgValue);
                        //生产者把消息发送出去，几个枚举参数MsgDeliveryMode是否长链，MsgPriority消息优先级别，发送最小单位，当然还有其他重载
                        await prod.SendAsync(message, msgDeliveryMode, MsgPriority.Normal, TimeSpan.MinValue);
                        //txtMessage = "发送成功!!";
                    });
                });
                return true;

            }
            catch (Exception)
            {
                return false;
            }

        }
        /// <summary>
        /// MQ通讯配置信息
        /// </summary>
        public class MQConfig : MQFactory.MQParameter
        {
            #region  配置属性 
            /// <summary>
            ///  msgId类型
            /// </summary>
            public string MsgKey { get; set; }
            /// <summary>
            ///  msgId值
            /// </summary>
            public string MsgValue { get; set; }
            /// <summary>
            /// MQ接收通道
            /// </summary>
            public string ReceiveQueue { get; set; }
            /// <summary>
            /// MQ发送通道
            /// </summary>
            public string SendQueue { get; set; }
            /// <summary>
            /// 发送标识
            /// </summary>
            public string SendId { get; set; } = Guid.NewGuid().ToString();
            /// <summary>
            /// 接收标识
            /// </summary>
            public string ReceiveId { get; set; }
            /// <summary>
            /// ActiveMQ异步发送 默认同步发送
            /// </summary>
            public bool UseAsyncSend { get; set; } = false;
            /// <summary>
            /// 最大重连间隔 默认为 3000ms
            /// </summary>
            public int ReConnectionTike { get; set; } = 3000;
            /// <summary>
            /// 是否议题模式
            /// </summary>
            public bool IsTopic = false;
            ///// <summary>
            ///// 持久化订阅议题
            ///// </summary>
            //public bool IsTopicDurable = false;
            /// <summary>
            /// ActiveMQ消息类型
            /// </summary>
            public MqMessageTypeEnum TypeEnum { get; set; } = MqMessageTypeEnum.Queue;



            #endregion

            #region ActiveMQ消息类型
            /// <summary>
            /// ActiveMQ消息类型
            /// </summary>
            public enum MqMessageTypeEnum
            {
                /// <summary>
                /// 主题，广播消息。生产者发布主题，所有订阅者都将收到该主题。Topic方式适合作为消息分发或者消息多用途时使用。使用Apache-ActiveMQ实现消息传送服，所有消息获取端都可以获得相同的消息。
                /// </summary>
                Topic,
                /// <summary>
                /// 消息队列。点对点模式。Queues更适合做负载均衡，让多个消息获取端处理一组消息。使用Apache-ActiveMQ实现消息传送服，Queues方式下消息只能被获取一次，多个消息获取端不会获得重复的信息。
                /// </summary>
                Queue
            }
            #endregion

            #region  配置 
            /// <summary>
            /// 读取MqConfig文件
            /// </summary>
            /// <param name="filePath">配置文件路径</param>
            /// <returns>返回配置对象</returns>
            public static MQConfig Load(string filePath = "ConfigInfo\\MQConfig.xml")
            {
                try
                {
                    XmlSerializer xs = new XmlSerializer(typeof(MQConfig));
                    using XmlTextReader xr = new XmlTextReader(filePath);
                    MQConfig rtn = (MQConfig)xs.Deserialize(xr);
                    xr.Close();
                    return rtn;
                }
                catch (Exception) { return null; }
            }

            /// <summary>
            /// 保存MqConfig文件
            /// </summary>
            /// <param name="config">配置对象</param>
            /// <param name="filePath">配置文件路径</param>

            public static void Save(MQConfig config, string filePath = "MQConfig.xml")
            {
                try
                {
                    XmlSerializer xs = new XmlSerializer(typeof(MQConfig));
                    using XmlTextWriter tw = new XmlTextWriter(filePath, Encoding.UTF8) { Indentation = 4, Formatting = Formatting.Indented };
                    xs.Serialize(tw, config);
                    tw.Flush();
                    tw.Close();
                }
                catch (Exception) { }
            }
            #endregion

            /// <summary>
            /// 消息类型
            /// </summary>
            public enum MessageType
            {
                IBytesMessage,
                ITextMessage,
                IMapMessage,
                IObjectMessage,
                IStreamMessage
            }
            /// <summary>
            /// 通讯信息
            /// </summary>
            public class MqMessage
            {
                public string CorrelationID { get; set; }
                public string DeliveryMode { get; set; }
                public string MessageId { get; set; }
                public bool Redelivered { get; set; }
                public DateTime Timestamp { get; set; }
                public TimeSpan TimeToLive { get; set; }
                public string Type { get; set; }
                public object Content { get; set; }
                public MessageType ContentType { get; set; }

            }
        }
    }
}
