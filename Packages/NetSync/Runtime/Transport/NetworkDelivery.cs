using System.Collections;
using System.Collections.Generic;

namespace Yanmonet.Network.Sync
{

    public enum NetworkDelivery
    {
        /// <summary>
        /// ���򲻿ɿ���Ϣ
        /// </summary>
        Unreliable,
        /// <summary>
        /// ���򲻿ɿ���
        /// </summary>
        UnreliableSequenced,
        /// <summary>
        /// �ɿ�����Ϣ
        /// </summary>
        Reliable,
        /// <summary>
        /// �ɿ������
        /// </summary>
        ReliableSequenced,
    }

}
