using System.Collections.Generic;
using System.Threading.Tasks;

namespace SuperMQ.MQFactory
{
    public interface IMQClient
    {
        /// <summary>
        /// 启动
        /// </summary>
        void Start();
        /// <summary>
        /// 停止
        /// </summary>
        void Stop();
        /// <summary>
        /// 释放
        /// </summary>
        void Dispose();
        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="Topic">发布主题</param>
        /// <param name="message">发布内容</param>
        /// <returns></returns>
        Task<bool> Publish(List<string> topics, string message);
        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="message">发布内容</param>
        /// <returns></returns>
        Task<bool> Publish(string message);
        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="Topic">发布主题</param>
        /// <param name="message">发布内容</param>
        /// <returns></returns>
        Task<bool> Publish(string topic, string message);
    }
}
