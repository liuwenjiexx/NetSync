using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Net
{

    public class NetworkReader
    {
        private MemoryStream msReader;
        private Socket baseStream;

        public NetworkReader(Socket socket)
        {
            this.baseStream = socket;
            msReader = new MemoryStream(100);
        }

        private ushort packageSIze;
        public MemoryStream ReaderStream { get { return msReader; } }

        public ushort PackageSize { get { return packageSIze; } }

        public ushort ReadPackage()
        {
            if (packageSIze > 0)
            {
                return ReadPackageContent();
            }

            if (baseStream.Available > 2)
            {
                
                byte[] buff = msReader.GetBuffer();
                baseStream.Receive(buff, 2, SocketFlags.None);
                packageSIze = (ushort)(buff[0] << 8);
                packageSIze |= (ushort)(buff[1]);

                msReader.Position = 0;
                msReader.SetLength(packageSIze);
                
                return ReadPackageContent();
            }
            return 0;
        }
        private ushort ReadPackageContent()
        {
            if (packageSIze > 0)
            {

                int count = packageSIze - (int)msReader.Position;
                if (count > 0)
                {
                    int readCount;
                    readCount = baseStream.Receive(msReader.GetBuffer(), (int)msReader.Position, count, SocketFlags.None);
                    if (readCount > 0)
                    {
                        msReader.Position += readCount;
                    }
                }
                if (msReader.Position >= packageSIze)
                {
                    var tmp = packageSIze;
                    packageSIze = 0;
                    msReader.Position = 0;
                    return tmp;
                }
            }
            return 0;
        }



    }
}
