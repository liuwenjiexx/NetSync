using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using Yanmonet.NetSync.Messages;

namespace Yanmonet.NetSync.Test.Connection
{
    [TestClass]
    public class SendMessage : TestConnectionBase
    {
        [TestMethod]
        public void Empty()
        {
            bool success = false;
            using (NewConnect(out var serverConn, out var clientConn))
            using (serverConn)
            using (clientConn)
            {

                clientConn.RegisterHandler((ushort)NetworkMsgId.Max, (netMsg) =>
                {
                    success = true;
                });
                serverConn.SendMessage((ushort)NetworkMsgId.Max, null);
                Update(serverConn, clientConn);

                Assert.IsTrue(success);

            }
        }


        [TestMethod]
        public void String()
        {      
            bool success = false;
            using (NewConnect(out var serverConn, out var clientConn))
            using (serverConn)
            using (clientConn)
            {
                clientConn.RegisterHandler((ushort)NetworkMsgId.Max, (netMsg) =>
                {
                    var msg = netMsg.ReadMessage<StringMessage>();
                    Assert.AreEqual(msg.Value, "abc");
                    success = true;
                });
                serverConn.SendMessage((ushort)NetworkMsgId.Max, new StringMessage("abc"));
           
                Update(serverConn, clientConn);

                Assert.IsTrue(success);
            }
        }


    }
}
