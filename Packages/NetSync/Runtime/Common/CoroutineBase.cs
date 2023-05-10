//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Threading;

//namespace Yanmonet.Network.Sync
//{
//    public class CoroutineBase
//    {
//        private static LinkedList<CoroutineState> routines;
//        private static int mainThreadId;


//        protected CoroutineBase()
//        {


//        }

//        protected static bool IsThreadSafe
//        {
//            get { return mainThreadId == 0 || mainThreadId == Thread.CurrentThread.ManagedThreadId; }
//        }


//        protected static void CheckThreadSafe()
//        {
//            if (!IsThreadSafe)
//            {
//                if (mainThreadId == 0)
//                    return;
//                throw new Exception("not main thread " + mainThreadId + "," + Thread.CurrentThread.ManagedThreadId);
//            }
//        }

//        #region Coroutine

//        public static void ResetCoroutine()
//        {
//            mainThreadId = Thread.CurrentThread.ManagedThreadId;
//            if (routines == null)
//                routines = new LinkedList<CoroutineState>();
//            else
//                routines.Clear();
//        }

//        public static void UpdateCoroutine()
//        {
//            CheckThreadSafe();

//            if (routines != null)
//            {
//                var current = routines.First;
//                CoroutineState state;
//                while (current != null)
//                {
                
//                        state = current.Value;
//                        if (/*state.coroutine.Target == null ||*/ !state.routine.MoveNext())
//                        {
//                            if (current.List != null)
//                            {
//                                current.List.Remove(current);
//                                current = current.Next;
//                            }
//                            else
//                            {
//                                current = null;
//                            }
//                            continue;
//                        }
              
//                    current = current.Next;
//                }
//            }
//        }

//        public void StartCoroutine(IEnumerator routine)
//        {
//            if (routine.MoveNext())
//            {
//                if (routines == null)
//                    routines = new LinkedList<CoroutineState>();
//                routines.AddLast(new CoroutineState()
//                {
//                    coroutine = new WeakReference(this),
//                    routine = routine
//                });
//            }
//        }

//        #endregion

//        class CoroutineState
//        {
//            public System.WeakReference coroutine;
//            public IEnumerator routine;
//        }

//    }
//}
