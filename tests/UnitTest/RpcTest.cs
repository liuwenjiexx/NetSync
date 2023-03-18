using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Yanmonet.NetSync.Test
{
    [TestClass]
    public class RpcTest : TestBase
    {
      
        [TestMethod]
        public void Test1()
        {
            Type type = typeof(TestStruct);
            var typeCode = Type.GetTypeCode(type);
            type = typeof(string);
            type = typeof(int);
            bool b= typeof(string).IsPrimitive;
        }


        [TestMethod]
        public void RefPerformance()
        {
            int max = 10000000;
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            TestStruct s;
            s = new TestStruct();
            sw.Start();
            for (int i = 0; i < max; i++)
            {
                Serialize(s);
            }
            sw.Stop();
            Console.WriteLine("time:" + sw.ElapsedMilliseconds);
            sw.Reset();
            sw.Start();
            for (int i = 0; i < max; i++)
            {
                SerializeRef(ref s);
            }
            sw.Stop();
            Console.WriteLine("ref time:" + sw.ElapsedMilliseconds);

        }
        struct TestStruct
        {
            public float f1;
            public int i1;
            public float f2;
            //public float f3;
            //public float f4;
            //public float f5;
            //public float f6;
            //public float f7;
            //public float f8;
            //public float f9;
            //public float f10;
        }
        void SerializeRef(ref TestStruct value)
        {
            value.f1 = 1f;
            //value.i1 = 2;
            //value.f1 = 3f;
        }

        void Serialize(TestStruct value)
        {
            value.f1 = 1f;
            //value.i1 = 2;
            //value.f1 = 3f;
        }


        [TestMethod]
        public async Task ThreadId_Test()
        {
            Console.WriteLine("Yield Before ThreadId: " + Thread.CurrentThread.ManagedThreadId);
            await Task.Yield();
            Console.WriteLine("Yield After ThreadId: " + Thread.CurrentThread.ManagedThreadId);
            Console.WriteLine("Delay Before ThreadId: " + Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(10);
            Console.WriteLine("Delay After ThreadId: " + Thread.CurrentThread.ManagedThreadId);


            await Task.Delay(10);
            Console.WriteLine("ThreadId: " + Thread.CurrentThread.ManagedThreadId);

            await Task.Delay(10);
            Console.WriteLine("ThreadId: " + Thread.CurrentThread.ManagedThreadId);

            await Task.Delay(10);
            Console.WriteLine("ThreadId: " + Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(10);
            Console.WriteLine("ThreadId: " + Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(10);
            Console.WriteLine("ThreadId: " + Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(10);
            Console.WriteLine("ThreadId: " + Thread.CurrentThread.ManagedThreadId);
        }


    }

    sealed class SingleThreadSynchronizationContext : SynchronizationContext
    {
        private readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object>> m_queue = new();

        public override void Post(SendOrPostCallback d, object state)
        {
            m_queue.Add(new KeyValuePair<SendOrPostCallback, object>(d, state));
        }

        public void RunOnCurrentThread()
        {
            KeyValuePair<SendOrPostCallback, object> workItem;

            while (m_queue.TryTake(out workItem, Timeout.Infinite))
                workItem.Key(workItem.Value);
        }

        public void Complete() { m_queue.CompleteAdding(); }

    }
}
