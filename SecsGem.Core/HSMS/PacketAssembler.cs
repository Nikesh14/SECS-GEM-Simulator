using SecsGem.Core.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SecsGem.Core.HSMS
{
    public class PacketAssembler
    {
        private static readonly long MaxbyteLength = 102400;
        private readonly List<byte> _byteBuffer;
        public PacketAssembler()
        {
            _byteBuffer = new List<byte>();
        }
        public List<byte[]> AssemblePackets(byte[] byteReceived)
        {
            _byteBuffer.AddRange(byteReceived);
            bool possibleToCreatePacket = false;
            if (_byteBuffer.Count >= 4)
                possibleToCreatePacket = true;
            var assembledPackets = new List<byte[]>();
            while (possibleToCreatePacket)
            {
                var lengthbyteArray = _byteBuffer.Take(4).ToArray();
                var packetLength = GetLength(lengthbyteArray);
                if (packetLength > MaxbyteLength)
                    throw new Exception($"Length of transmission is greater than the maximum length allowed!");
                if (_byteBuffer.Count >= packetLength + 4)
                {
                    var reqPacket = _byteBuffer.Skip(4).Take((int)packetLength).ToArray();
                    assembledPackets.Add(reqPacket);
                    _byteBuffer.RemoveRange(0, (int)(packetLength + 4));
                }
                else
                {
                    break;
                }
                if (_byteBuffer.Count < 4)
                    possibleToCreatePacket = false;
            }
            return assembledPackets;
        }

        private long GetLength(byte[] bytes)
        {
            long length = 0;
            for(int i=0; i<bytes.Length; ++i)
            {
                length += bytes[i] * (int)Math.Pow(256, bytes.Length - (i + 1));
            }
            return length;
        }

       
    }
}
