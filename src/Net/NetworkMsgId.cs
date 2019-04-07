using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Net
{
    public enum NetworkMsgId
    {
        /// <summary>
        /// 协议握手
        /// </summary>
        Handshake = 1,
        /// <summary>
        /// Ping 测试延迟用
        /// </summary>
        Ping,
        /// <summary>
        /// 创建实例
        /// </summary>
        CreateObject,
        DestroyObject,
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
