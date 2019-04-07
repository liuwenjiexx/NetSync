using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Net
{
    internal static class Utils
    {

        private static readonly DateTime timestamp1970 = new DateTime(1970, 1, 1, 0, 0, 0);

        public static long Timestamp
        {
            get { return (long)((DateTime.UtcNow - timestamp1970).TotalMilliseconds); }
        }

        public static int IndexOf<T>(this T[] source, Func<T, bool> match)
        {
            for (int i = 0, len = source.Length; i < len; i++)
            {
                if (match(source[i]))
                    return i;
            }
            return -1;
        }

        public static LinkedListNode<T> RemoveAndNext<T>(this LinkedListNode<T> source)
        {
            var next = source.Next;
            if (source.List != null)
                source.List.Remove(source);
            return next;
        }

        /// <summary>
        /// 000=0, 001=1, 010=2
        /// </summary>
        /// <param name="bits"></param>
        /// <returns></returns>
        internal static byte SigleBitPosition(this uint bits)
        {
            if (bits == 0)
                return 0;
            byte length = 0;
            while (bits != 0)
            {
                bits >>= 1;
                length++;
            }
            return length;
        }

    }

}
