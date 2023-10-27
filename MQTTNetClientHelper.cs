using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SuperMQ.MQFactory.MQParameter;
using static SuperMQ.SuperMQTT.MQTTNetClientHelper.MQTTParameter;

namespace SuperMQ.SuperMQTT
{
    /// <summary>
    /// MQTTnet
    /// </summary>
    public class MQTTNetClientHelper : MQFactory.IMQClient
    {

        #region 委托事件
        /// <summary>
        /// 连接状态改变委托
        /// </summary>
        /// <param name="state"></param>
        public delegate void ClientConnectionChange(ConnectedState state, string info, MqttClientConnectedEventArgs connectResult = null, MqttClientDisconnectedEventArgs eventArgs = null);
        /// <summary>
        /// 连接状态发生改变事件
        /// </summary>
        public event ClientConnectionChange ClientConnectionEvent;
        /// <summary>
        /// 接受消息委托
        /// </summary>
        /// <param name="message"></param>
        public delegate void ClientMessageReceived(MessageInfo message);
        /// <summary>
        /// 接受消息事件
        /// </summary>
        public event ClientMessageReceived ClientMessageReceivedEvent;
        /// <summary>
        /// 发送消息状态委托
        /// </summary>
        /// <param name="info"></param>
        public delegate void ClientSendMessage(bool result, string info);
        /// <summary>
        /// 发送消息状态事件
        /// </summary>
        public event ClientSendMessage ClientSendMessageEvent;
        public delegate void ExceptionHappened(Exception exception);
        /// <summary>
        /// 发生异常事件
        /// </summary>
        public event ExceptionHappened ExceptionHappenedEvent;
        public delegate void CertificateValidation(MqttClientCertificateValidationEventArgs arg);
        /// <summary>
        /// 证书校验结果
        /// </summary>
        public event CertificateValidation CertificateValidationEvent;
        #endregion

        #region 全局对象
        /// <summary>
        /// 
        /// </summary>
        MqttClient mqttClient = null;
        /// <summary>
        /// 连接参数
        /// </summary>
        public MqttClientOptions options = null;
        private bool workState = false;
        private bool running = false;

        public MQTTParameter Parameter { get; private set; }
#if NET462_OR_GREATER
        /// <summary>
        /// 加密协议
        /// </summary>
        public SslProtocols SslProtocol = SslProtocols.Default;
#endif
#if NET6_0_OR_GREATER
        /// <summary>
        /// 加密协议
        /// </summary>
        public SslProtocols SslProtocol = SslProtocols.None; //SslProtocols.Tls | SslProtocols.Tls13;
#endif
        /// <summary>
        /// 是否在运行
        /// </summary>
        /// <returns></returns>
        public bool IsRun => workState && running;

        /// <summary>
        /// mq 协议版本
        /// </summary>
        public MqttProtocolVersion MqttProtocol { get; set; } = MqttProtocolVersion.V311;
        /// <summary>
        /// 证书列表
        /// </summary>
        public List<X509Certificate> x509Certificates = null;// new X509Certificate(@"C:\cert.pfx", "psw");

        #endregion

        /// <summary>
        /// 构造实例
        /// </summary>
        /// <param name="mqttParameter">参数</param>
        public MQTTNetClientHelper(MQTTParameter mqttParameter)
        {
            Parameter = mqttParameter;
        }
        /// <summary>
        /// 启动客户端
        /// </summary>
        public async void Start()
        {

            Console.WriteLine("MQTT Work >>Begin");
            workState = true;

            await WorkMqttClient();

        }
        /// <summary>
        /// 停止
        /// </summary>
        public async void Stop()
        {
            workState = false;

            if (mqttClient != null)
                await mqttClient.DisconnectAsync().ConfigureAwait(false);

            Console.WriteLine("MQTT Work >>End");
        }

        public void Dispose()
        {
            mqttClient?.Dispose();
            mqttClient = null;
        }

        /// <summary>
        /// 工作
        /// </summary>
        private async Task WorkMqttClient()
        {

            try
            {
                mqttClient = new MqttFactory().CreateMqttClient() as MqttClient;
                MqttClientOptionsBuilder mqttClientOptions = new MqttClientOptionsBuilder();
                mqttClientOptions = Parameter.MqttType switch
                {
                    MQType.WebSocket => new MqttClientOptionsBuilder()
                                        .WithWebSocketServer(o=>o.WithUri(Parameter.ServerUrl) ),
                    _ => new MqttClientOptionsBuilder()
                                       .WithTcpServer(Parameter.ServerUrl, Parameter.Port),
                };
                if (x509Certificates != null)
                {
                    mqttClientOptions = mqttClientOptions.WithTlsOptions(o => 
                    {
                        o.WithCertificateValidationHandler(CertificateValidationCallback);
                        o.WithSslProtocols(SslProtocol);
                    });
                }
                options = mqttClientOptions.WithCredentials(Parameter.User, Parameter.Password)
                    .WithClientId(Parameter.ClientId)
                    .WithCleanSession()
                    .WithKeepAlivePeriod(Parameter.KeepAlive)
                    .WithProtocolVersion(MqttProtocol)
                    .Build();

                mqttClient.ConnectedAsync += Connected;
                mqttClient.DisconnectedAsync += Disconnected;
                mqttClient.ApplicationMessageReceivedAsync += MqttApplicationMessageReceived;
                using var timeout = new CancellationTokenSource(5000);
                await mqttClient.ConnectAsync(options, timeout.Token);
                Console.WriteLine("The MQTT client is connected.");
            }
            catch (Exception exp)
            {

                Console.WriteLine("Work >> " + exp);
                running = false;
                workState = false;
                ExceptionHappenedEvent?.Invoke(exp);
            }
        }
        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="Message">发布内容</param>
        /// <returns></returns>
        public async Task<bool> Publish(List<string> topics, string message)
        {
            try
            {
                if (topics == null || topics.Count == 0 || string.IsNullOrWhiteSpace(message))
                    return false;
                if (mqttClient == null) return false;
                if (mqttClient.IsConnected == false)
                    await mqttClient.ConnectAsync(options).ConfigureAwait(false);

                if (mqttClient.IsConnected == false)
                {
                    ClientSendMessageEvent?.Invoke(false, "连接失败");                    
                    Console.WriteLine("Publish >>Connected Failed! ");
                    return false;
                }

                MqttApplicationMessageBuilder mamb = new MqttApplicationMessageBuilder()
                 .WithPayload(message)
                 .WithRetainFlag(Parameter.Retained)
                 .WithQualityOfServiceLevel(Parameter.QualityOfServiceLevel);
                topics?.ForEach(async topic => 
                {
                    mamb = mamb.WithTopic(topic);
                    await mqttClient.PublishAsync(mamb.Build());
                });
               
                Console.WriteLine($"Publish >>Topic: {string.Join(",", Parameter.Topics?.ToArray())}; QoS: {Parameter.QualityOfServiceLevel}; Retained: {Parameter.Retained};");
                Debug.WriteLine("Publish >>Message: " + message);
                ClientSendMessageEvent?.Invoke(true, "发送完成");
                return true;
            }
            catch (Exception exp)
            {
                Console.WriteLine("Publish >>" + exp.Message);
                ExceptionHappenedEvent?.Invoke(exp);
                return false;
            }
        }
        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="Message">发布内容</param>
        /// <returns></returns>
        public async Task<bool> Publish(string Message) => await Publish(Parameter.Topics, Message);
        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="Topic">发布主题</param>
        /// <param name="Message">发布内容</param>
        /// <returns></returns>
        public async Task<bool> Publish(string topic, string Message) => await Publish(new List<string>() { topic }, Message);

        #region event

        private bool CertificateValidationCallback(MqttClientCertificateValidationEventArgs arg)
        {
            CertificateValidationEvent?.Invoke(arg);
            return true;
        }

        /// <summary>
        /// 连接服务器并按标题订阅内容
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task Connected(MqttClientConnectedEventArgs e)
        {
            try
            {
                running = true;
                List<MqttTopicFilter> listTopic = new List<MqttTopicFilter>();
                Console.ForegroundColor = ConsoleColor.Green;
                Parameter.TopicsConsumer.ForEach(o =>
                {
                    var topicFilterBulder = new MqttTopicFilterBuilder().WithTopic(o).Build();
                    listTopic.Add(topicFilterBulder);
                    Console.WriteLine("Connected >>Subscribe " + o);
                });
                MqttClientSubscribeOptions options = new MqttClientSubscribeOptions() { TopicFilters = listTopic };
                await mqttClient.SubscribeAsync(options).ConfigureAwait(false);
                ClientConnectionEvent?.Invoke(ConnectedState.Connected, "连接成功", e);
                
                Console.WriteLine("Connected >>Subscribe Success");
                Console.ResetColor();
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.Message);
                ClientConnectionEvent?.Invoke(ConnectedState.Exception, exp.Message);

            }
        }
        /// <summary>
        /// 失去连接触发事件
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task Disconnected(MqttClientDisconnectedEventArgs e)
        {
            try
            {
                ClientConnectionEvent?.Invoke(ConnectedState.DisConnected, "失去连接", null, e);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Disconnected >>Disconnected Server({e.Reason};{e.Exception})");
                Console.ResetColor();
                if (workState)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                    try
                    {
                        await mqttClient.ConnectAsync(options).ConfigureAwait(false);
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine("Disconnected >>Exception " + exp.Message);
                    }
                }
                else
                {
                    running = false;

                }
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.Message);
                ExceptionHappenedEvent?.Invoke(exp);

            }
        }
        /// <summary>
        /// 接收消息触发事件
        /// </summary>
        /// <param name="e"></param>
        private async Task MqttApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                string text = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                string Topic = e.ApplicationMessage.Topic;
                string QoS = e.ApplicationMessage.QualityOfServiceLevel.ToString();
                string Retained = e.ApplicationMessage.Retain.ToString();
                ClientMessageReceivedEvent?.Invoke(new MessageInfo() { QoS = e.ApplicationMessage.QualityOfServiceLevel.ToString(), Retained = e.ApplicationMessage.Retain.ToString(), Text = Encoding.UTF8.GetString(e.ApplicationMessage.Payload), Topic = e.ApplicationMessage.Topic });

                Console.WriteLine("MessageReceived >>Topic:" + Topic + "; QoS: " + QoS + "; Retained: " + Retained + ";");
                Debug.WriteLine("MessageReceived >>Msg: " + text);

                await Task.CompletedTask;
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.Message);
                ExceptionHappenedEvent?.Invoke(exp);
            }
        }

        #endregion

        #region classes
        /// <summary>
        /// 参数
        /// </summary>
        public class MQTTParameter : MQFactory.MQParameter
        {
            public MQType MqttType { get; set; }
            /// <summary>
            /// 服务质量
            /// <para>0 - 至多一次</para>
            /// <para>1 - 至少一次</para>
            /// <para>2 - 刚好一次</para>
            /// </summary>
            public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; } = 0;

            /// <summary>
            /// 消息体 
            /// </summary>
            public class MessageInfo
            {
                public string Text { get; set; }
                public string Topic { get; set; }
                public string QoS { get; set; }
                public string Retained { get; set; }
            }
            public enum MQType
            {
                MQTT,
                WebSocket,
                Tcp
            }
        }
        #endregion

    }

}
