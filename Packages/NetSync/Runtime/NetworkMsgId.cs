using System;

namespace Yanmonet.NetSync
{
    public enum NetworkMsgId
    {
        /// <summary>
        /// 连接
        /// </summary>
        ConnectRequest = 1,
        ConnectResponse,
        /// <summary>
        /// 断开连接消息
        /// </summary>
        Disconnect,
        /// <summary>
        /// Ping 测试延迟用
        /// </summary>
        Ping,
        /// <summary>
        /// 创建实例
        /// </summary>
        CreateObject,
        DestroyObject,
        Spawn,
        /// <summary>
        /// 同步变量
        /// </summary>
        SyncVar,
        /// <summary>
        /// 同步 List
        /// </summary>
        SyncList,
        /// <summary>
        /// 同步事件 
        /// </summary>
        SyncEvent,
        Rpc,
        Max = 10,
    }
}
