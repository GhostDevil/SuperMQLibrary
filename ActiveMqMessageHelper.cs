using Apache.NMS;
using Apache.NMS.ActiveMQ;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SuperMQ
{
    /// <summary>
    /// ActiveMq消息总线
    /// </summary>
    public class ActiveMqMessageHelper : MQFactory.IMQClient, IDisposable
    {
        #region 成员变量

        /// <summary>
        /// ActiveMQ消息类型
        /// </summary>
        public enum MqMessageTypeEnum
        {
            /// <summary>
            /// 主题。生产者发布主题，所有订阅者都将收到该主题。
            /// </summary>
            Topic,
            /// <summary>
            /// 消息队列。点对点模式。
            /// </summary>
            Queue
        }

        private bool isInitSuccess = false; // 队列消息消费者是否成功初始化
        private IConnection connection;
        private ISession session;
        /// <summary>
        /// 生产者消息总线地址
        /// </summary>
        public string UriProvider { get; set; }
        /// <summary>
        /// 主题或消息队列
        /// </summary>
        public string MessageName { get; set; }
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }


        #endregion

        #region 构造函数

        public ActiveMqMessageHelper()
        {

        }

        public ActiveMqMessageHelper(string messageName, string uriProvider)
            : this()
        {
            UriProvider = uriProvider;
            MessageName = messageName;
        }

        public ActiveMqMessageHelper(string messageName, string uriProvider, string userName, string password)
            : this(messageName, uriProvider)
        {
            UserName = userName;
            Password = password;
        }

        ~ActiveMqMessageHelper()
        {
            Stop();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化队列消息消费者
        /// </summary>
        public async void Start()
        {
            try
            {
                if (string.IsNullOrEmpty(UriProvider))
                {
                    throw new ArgumentNullException("生产者消息总线地址为空！");
                }
                if (isInitSuccess)
                {
                    Stop();
                }

                // 得到连接工厂
                ConnectionFactory factory = new ConnectionFactory(UriProvider);
                // 创建一个连接
                if (!string.IsNullOrEmpty(UserName))
                {
                    connection = factory.CreateConnection(UserName, Password);
                }
                else
                {
                    connection = factory.CreateConnection();
                }
                // 打开连接
                await connection.StartAsync();
                // 得到会话
                session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
                // 初始化完成
                isInitSuccess = true;
            }
            catch(Exception)
            {
                isInitSuccess = false;
                throw;
            }
        }

        /// <summary>
        /// 停止队列消息消费者，释放资源
        /// </summary>
        public async void Stop()
        {
            try
            {
                if (!isInitSuccess)
                    return;

                if (session != null)
                {
                    await session.CloseAsync();

                }

                if (connection != null)
                {
                    await connection.StopAsync();
                    await connection.CloseAsync();

                }
            }
            catch (Exception)
            {
                throw;
            }
        }


        /// <summary>
        /// 创建消费者
        /// </summary>
        /// <param name="messageType">消息类型</param>
        /// <param name="messageName">主题或消息队列</param>
        public IMessageConsumer CreateConsumer(MqMessageTypeEnum messageType, string messageName) => CreateConsumer(messageType, messageName, null);

        /// <summary>
        /// 创建消费者
        /// </summary>
        /// <param name="messageType">消息类型</param>
        public IMessageConsumer CreateConsumer(MqMessageTypeEnum messageType) => CreateConsumer(messageType, MessageName, null);


        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="messageBody">消息消息主体param>
        /// <param name="producer">生产者</param>
        public void Publish(IDictionary<string, object> messageBody, IMessageProducer producer)
        {
            try
            {
                if (!isInitSuccess)
                {
                    Start();
                }
                IMapMessage mapMsg = session.CreateMapMessage();
                if (mapMsg == null)
                {
                    return;
                }

                SetMessageBody(messageBody, ref mapMsg);

                //发送消息
                producer.Send(mapMsg, MsgDeliveryMode.Persistent, MsgPriority.High, TimeSpan.MinValue);
            }
            catch(Exception)
            {
                isInitSuccess = false;
                throw;
            }
        }



        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="messageBody">消息主体</param>
        /// <param name="messageType">消息类型</param>
        public void Publish(IDictionary<string, object> messageBody, MqMessageTypeEnum messageType)
        {
            try
            {
                if (!isInitSuccess)
                {
                    Start();
                }
                using IMessageProducer producer = CreateProducer(messageType);
                Publish(messageBody, producer);
            }
            catch(Exception)
            {
                isInitSuccess = false;
                throw;
            }
        }



        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="messageText">消息文本</param>
        /// <param name="messageProperties">消息属性</param>
        /// <param name="producer">生产者</param>
        public void Publish(string messageText, IDictionary<string, object> messageProperties, IMessageProducer producer)
        {
            try
            {
                if (!isInitSuccess)
                {
                    Start();
                }
                ITextMessage textMsg = session.CreateTextMessage();
                if (textMsg == null)
                {
                    return;
                }
                textMsg.Text = messageText;
                if (messageProperties != null && messageProperties.Count > 0)
                {
                    SetMessageProperties(messageProperties, ref textMsg);
                }
                //发送消息
                producer.Send(textMsg, MsgDeliveryMode.Persistent, MsgPriority.High, TimeSpan.MinValue);
            }
            catch (Exception)
            {
                isInitSuccess = false;
                throw;
            }
        }
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="messageText">消息文本</param>
        /// <param name="messageProperties">消息属性</param>
        /// <param name="messageType">消息类型</param>
        public void Publish(string messageText, IDictionary<string, object> messageProperties, MqMessageTypeEnum messageType)
        {
            try
            {
                if (!isInitSuccess)
                {
                    Start();
                }
                using IMessageProducer producer = CreateProducer(messageType);
                Publish(messageText, messageProperties, producer);
            }
            catch (Exception)
            {
                isInitSuccess = false;
                throw;
            }
        }
        public Task<bool> Publish(List<string> topics, string Message)
        {
            return Task.Run(() =>
            {
                topics?.ForEach(topic =>
                {
                    Publish(topic, Message);
                });
                return true;
            });
        }

        public Task<bool> Publish(string message)
        {
            return Task.Run(() =>
            {
                using IMessageProducer producer = CreateProducer(MqMessageTypeEnum.Queue);
                Publish(message, null, producer);
                return true;
            });
        }

        public Task<bool> Publish(string topic, string message)
        {
            return Task.Run(() =>
            {
                MessageName = topic;
                using (IMessageProducer producer = CreateProducer(MqMessageTypeEnum.Topic, MessageName))
                {
                    Publish(message, null, producer);
                }
                return true;
            });

        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 创建生产者
        /// </summary>
        /// <param name="messageType">消息类型</param>
        /// <param name="messageName">主题或消息队列</param>
        IMessageProducer CreateProducer(MqMessageTypeEnum messageType, string messageName)
        {
            try
            {
                if (!isInitSuccess)
                {
                    Start();
                }
                if (messageType == MqMessageTypeEnum.Topic)
                {
                    return session.CreateProducer(new Apache.NMS.ActiveMQ.Commands.ActiveMQTopic(messageName));
                }
                else
                {
                    return session.CreateProducer(new Apache.NMS.ActiveMQ.Commands.ActiveMQQueue(messageName));
                }
                //IDestination destNation = SessionUtil.GetDestination(_session, _messageName, DestinationType.Queue);
                //return _session.CreateProducer(destNation);
            }
            catch
            (Exception)
            {
                isInitSuccess = false;
                throw;
            }
        }

        /// <summary>
        /// 创建生产者
        /// </summary>
        /// <param name="messageType">消息类型</param>
        IMessageProducer CreateProducer(MqMessageTypeEnum messageType)
        {
            return CreateProducer(messageType, MessageName);
        }

        /// <summary>
        /// 创建消费者
        /// </summary>
        /// <param name="messageType">消息类型</param>
        /// <param name="messageName">主题或消息队列</param>
        /// <param name="selector">筛选器</param>
        IMessageConsumer CreateConsumer(MqMessageTypeEnum messageType, string messageName, string selector)
        {
            try
            {
                if (!isInitSuccess)
                {
                    Start();
                }
                if (string.IsNullOrEmpty(selector))
                {
                    if (messageType == MqMessageTypeEnum.Topic)
                    {
                        return session.CreateConsumer(new Apache.NMS.ActiveMQ.Commands.ActiveMQTopic(messageName));
                    }
                    else
                    {
                        return session.CreateConsumer(new Apache.NMS.ActiveMQ.Commands.ActiveMQQueue(messageName));
                    }
                }
                else
                {
                    if (messageType == MqMessageTypeEnum.Topic)
                    {
                        return session.CreateConsumer(new Apache.NMS.ActiveMQ.Commands.ActiveMQTopic(messageName), selector, false);
                    }
                    else
                    {
                        return session.CreateConsumer(new Apache.NMS.ActiveMQ.Commands.ActiveMQQueue(messageName), selector, false);
                    }
                }
            }
            catch (Exception)
            {
                isInitSuccess = false;
                throw;
            }
        }

        private static void SetMessageBody(IDictionary<string, object> messageBody, ref IMapMessage mapMsg)
        {
            foreach (KeyValuePair<string, object> item in messageBody)
            {
                switch (item.Value)
                {
                    case int @int:
                        mapMsg.Body.SetInt(item.Key, @int);
                        break;
                    case string @string:
                        mapMsg.Body.SetString(item.Key, @string);
                        break;
                    case bool boolean:
                        mapMsg.Body.SetBool(item.Key, boolean);
                        break;
                    case byte @byte:
                        mapMsg.Body.SetByte(item.Key, @byte);
                        break;
                    case char @char:
                        mapMsg.Body.SetChar(item.Key, @char);
                        break;
                    case IDictionary dictionary:
                        mapMsg.Body.SetDictionary(item.Key, dictionary);
                        break;
                    case double @double:
                        mapMsg.Body.SetDouble(item.Key, @double);
                        break;
                    case float single:
                        mapMsg.Body.SetFloat(item.Key, single);
                        break;
                    case IList list:
                        mapMsg.Body.SetList(item.Key, list);
                        break;
                    case short int1:
                        mapMsg.Body.SetShort(item.Key, int1);
                        break;
                }
            }
        }
        private static void SetMessageProperties(IDictionary<string, object> messageProperties, ref ITextMessage textMsg)
        {
            foreach (KeyValuePair<string, object> item in messageProperties)
            {
                switch (item.Value)
                {
                    case int @int:
                        textMsg.Properties.SetInt(item.Key, @int);
                        break;
                    case string @string:
                        textMsg.Properties.SetString(item.Key, @string);
                        break;
                    case bool boolean:
                        textMsg.Properties.SetBool(item.Key, boolean);
                        break;
                    case byte @byte:
                        textMsg.Properties.SetByte(item.Key, @byte);
                        break;
                    case char @char:
                        textMsg.Properties.SetChar(item.Key, @char);
                        break;
                    case IDictionary dictionary:
                        textMsg.Properties.SetDictionary(item.Key, dictionary);
                        break;
                    case double @double:
                        textMsg.Properties.SetDouble(item.Key, @double);
                        break;
                    case float single:
                        textMsg.Properties.SetFloat(item.Key, single);
                        break;
                    case IList list:
                        textMsg.Properties.SetList(item.Key, list);
                        break;
                    case short int1:
                        textMsg.Properties.SetShort(item.Key, int1);
                        break;
                }

            }
        }

        // 字符数组转字符串
        private static IList StringToByteList(string content)
        {
            return StringToByteList(content, "GB2312");
        }

        private static IList StringToByteList(string content, string encodeName)
        {
            byte[] resuleBytes = Encoding.GetEncoding(encodeName).GetBytes(content);
            return resuleBytes;
        }

        #endregion

        #region IDisposable 成员

        public void Dispose()
        {
            session?.Dispose();
            connection?.Dispose();
            GC.SuppressFinalize(this);
        }


        #endregion
    }

}
