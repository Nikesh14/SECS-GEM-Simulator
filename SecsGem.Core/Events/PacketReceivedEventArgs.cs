using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecsGem.Core.Events
{
    public sealed class PacketReceivedEventArgs
    {
        public PacketReceivedEventArgs(List<byte[]>packets)
        {
            Packets = packets;
        }
        public List<byte[]> Packets { get; }
    }
}
