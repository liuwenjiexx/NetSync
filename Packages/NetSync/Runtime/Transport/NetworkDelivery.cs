using System.Collections;
using System.Collections.Generic;

namespace Yanmonet.Network.Sync
{

    public enum NetworkDelivery
    {
        /// <summary>
        /// 无序不可靠消息
        /// </summary>
        Unreliable,
        /// <summary>
        /// 有序不可靠的
        /// </summary>
        UnreliableSequenced,
        /// <summary>
        /// 可靠的消息
        /// </summary>
        Reliable,
        /// <summary>
        /// 可靠有序的
        /// </summary>
        ReliableSequenced,
    }

}
