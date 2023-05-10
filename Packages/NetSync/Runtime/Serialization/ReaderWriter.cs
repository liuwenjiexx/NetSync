//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using UnityEngine;

//namespace Yanmonet.Network.Sync
//{
//    public struct Reader : IReaderWriter
//    {
//        private MemoryStream stream;

//        public Reader(MemoryStream stream)
//        {
//            this.stream = stream;
//        }

//        public bool IsReader => true;

//        public bool IsWriter => false;

//        public void SerializeValue(ref byte value)
//        {
//            throw new System.NotImplementedException();
//        }

//        public void SerializeValue(ref sbyte value)
//        {
//            throw new System.NotImplementedException();
//        }

//        public void SerializeValue(ref short value)
//        {
//            throw new System.NotImplementedException();
//        }

//        public void SerializeValue(ref ushort value)
//        {
//            throw new System.NotImplementedException();
//        }

//        public void SerializeValue(ref int value)
//        {
//            throw new System.NotImplementedException();
//        }

//        public void SerializeValue(ref uint value)
//        {
//            throw new System.NotImplementedException();
//        }

//        public void SerializeValue(ref long value)
//        {
//            throw new System.NotImplementedException();
//        }

//        public void SerializeValue(ref ulong value)
//        {
//            throw new System.NotImplementedException();
//        }

//        public void SerializeValue(ref float value)
//        {
//            throw new System.NotImplementedException();
//        }

//        public void SerializeValue(ref double value)
//        {
//            throw new System.NotImplementedException();
//        }

//        public void SerializeValue(ref bool value)
//        {
//            throw new System.NotImplementedException();
//        }

//        public void SerializeValue(ref string value)
//        {
//            throw new System.NotImplementedException();
//        }
//    }
//}