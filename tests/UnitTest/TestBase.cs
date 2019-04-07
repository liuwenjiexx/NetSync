using Microsoft.VisualStudio.TestTools.UnitTesting;
using Net;
using System.Collections;
using System.Collections.Generic;

namespace UnitTest
{


    public class TestBase
    {
        List<IEnumerator> runner;
     public   static string localMatchAddress = "localhost";
        public static int localMatchPort = 7000;
        public static string localHostAddress = "localhost";
        public static int localHostPort = 7001;
        public static string UserId = "userid";

        [TestInitialize]
        public virtual void TestInitialize()
        {
            runner = new List<IEnumerator>();
        }

        [TestCleanup]
        public virtual void TestCleanup()
        {
            CoroutineBase.UpdateCoroutine();
            CoroutineBase.UpdateCoroutine();
            CoroutineBase.UpdateCoroutine();
            CoroutineBase.UpdateCoroutine();
        }

        protected void Run(IEnumerator r)
        {
            runner.Add(r);

            while (runner.Count > 0)
            {
                CoroutineBase.UpdateCoroutine();
                for (int i = 0; i < runner.Count; i++)
                {
                    var item = runner[i];
                    if (!item.MoveNext())
                    {
                        runner.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        protected IEnumerable Wait(int frameCount)
        {
            while (frameCount-- > 0)
                yield return null;
        }

        protected IEnumerable Wait()
        {
            return Wait(6);
        }

    }
}
