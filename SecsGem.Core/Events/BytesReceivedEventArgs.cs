using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecsGem.Core.Events
{
    public sealed class BytesReceivedEventArgs : EventArgs
    {
        public BytesReceivedEventArgs(byte[] bytes)
        {
            Bytes = bytes;
        }
        public byte[] Bytes { get; }
    }
}
