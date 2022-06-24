using System;
using System.Collections.Generic;

namespace SuperMQ.MQFactory
{
    public abstract partial class MQParameter
    {
        /// <summary>
        /// 服务器IP
        /// </summary>
        public string ServerUrl { get; set; } 
        /// <summary>
        /// 服务器端口
        /// </summary>
        public int Port { get; set; } = 61613;
        /// <summary>
        /// 选项 - 开启登录 - 密码
        /// </summary>
        public string Password { get; set; } 
        /// <summary>
        /// 选项 - 开启登录 - 用户名
        /// </summary>
        public string User { get; set; } 
        /// <summary>
        /// 主题
        /// </summary>
        public List<string> Topics { get; set; }
        /// <summary>
        /// 保留消息
        /// </summary>
        public bool Retained { get; set; } = false;

        /// <summary>
        /// 客户端ID
        /// </summary>
        public string ClientId { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// 保活时间
        /// </summary>
        public TimeSpan KeepAlive { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// 连接状态
        /// </summary>
        public enum ConnectedState
        {
            /// <summary>
            /// 已连接
            /// </summary>
            Connected = 1,
            /// <summary>
            /// 断开连接
            /// </summary>
            DisConnected = 2,
            /// <summary>
            /// 连接异常
            /// </summary>
            Exception = 3,
        }

    }
}
