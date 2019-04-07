using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Net
{

    internal class Pool<T> : IDisposable
    {
        private List<State> list = new List<State>();
        private Dictionary<T, State> dic = new Dictionary<T, State>();
        private int currentIndex;
        private Func<T> factory;

        public Pool(Func<T> factory)
        {
            this.factory = factory;
        }

        public void Dispose()
        {
            for (int i = 0, len = list.Count; i < len; i++)
            {
                if (list[i].obj is IDisposable)
                {
                    ((IDisposable)list[i].obj).Dispose();
                }
            }
            list.Clear();
            dic.Clear();
        }

        public T Get()
        {
            int count = list.Count;
            int index;
            State s;
            for (int i = 0; i < count; i++)
            {
                index = (currentIndex + i) % count;
                s = list[index];
                if (!s.use)
                {
                    s.use = true;
                    currentIndex = index;
                    //s.obj.Position = 0;
                    //s.obj.SetLength(0);
                    return s.obj;
                }
            }
            s = new State()
            {
                obj = factory(),
                use = true,
            };
            list.Add(s);
            dic.Add(s.obj, s);
            currentIndex = list.Count - 1;
            return s.obj;
        }


        public void Unused(T ms)
        {
            State s;
            if (dic.TryGetValue(ms, out s))
            {
                s.use = false;
            }
        }



        private class State
        {
            public T obj;
            public bool use;
        }


    }
}
