using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using Net.Messages;

namespace UnitTest.Connection
{
    [TestClass]
    public class SendMessage : TestConnectionBase
    {
        [TestMethod]
        public void Empty()
        {
            Run(_SendMessage_Empty());
        }
        public IEnumerator _SendMessage_Empty()
        {
            bool success = false;
            Client.RegisterHandler((short)NetworkMsgId.Max, (netMsg) =>
            {
                success = true;
            });
            Server.SendMessage((short)NetworkMsgId.Max, null);
            foreach (var o in Wait()) yield return null;

            Assert.IsTrue(success);
        }


        [TestMethod]
        public void String()
        {
            Run(_SendMessage_String());
        }

        public IEnumerator _SendMessage_String()
        {
            bool success = false;
            Client.RegisterHandler((short)NetworkMsgId.Max, (netMsg) =>
            {
                var msg = netMsg.ReadMessage<StringMessage>();
                Assert.AreEqual(msg.Value, "abc");
                success = true;
            });
            Server.SendMessage((short)NetworkMsgId.Max, new StringMessage("abc"));
            foreach (var o in Wait()) yield return null;

            Assert.IsTrue(success);
        }


    }
}
