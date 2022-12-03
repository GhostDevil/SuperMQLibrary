using DotPulsar.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SuperMQ.Pulsar
{
    public abstract class PulsarBase
    {
        internal CancellationToken cancellationToken = default;
        internal CancellationTokenSource tokenSource;
    
        public EventHandler<Exception> Error;
        public IPulsarClient client = null;
        public string PubAddr { get; internal set; }
        public string ProductTopic { get; internal set; }
        public string Subscription { get; internal set; }
        /// <summary>
        /// 是否接收
        /// </summary>
        public bool IsReceive { get; internal set; } = false;
        /// <summary>
        /// 是否连接
        /// </summary>
        public bool IsConnected { get; internal set; } = false;

        public abstract Task<bool> ConnectAsync();
        public abstract Task<bool> DisConnectAsync();
    }
}
