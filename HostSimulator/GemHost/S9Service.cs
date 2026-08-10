using SecsGem.Core.Models;
using SecsGem.Core.SecIIMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostSimulator.GemHost
{
    public class S9Service
    {
        private readonly BinaryItem _binaryItem;

        public S9Service(HsmsMessage message)
        {
            var payload = new List<byte>();
            payload.AddRange(GenerateErrorPayload(message));
            _binaryItem = new BinaryItem(payload);
        }

        public SecsMessage SendS9F7()
        {
            return new S9F7(_binaryItem);
        }
        public SecsMessage SendS9F3()
        {
            return new S9F3(_binaryItem);
        }
        public SecsMessage SendS9F5()
        {
            return new S9F5(_binaryItem);
        }
        private byte[] GenerateErrorPayload(HsmsMessage _message)
        {
            List<byte> hsmsPacket = new List<byte>();
            // (highbyte)(256^1) + (lowbyte)(256^0) = deviceId
            // shifts the first 8 bytes to the right so the last 8 byte falls off giving us the high byte
            // and the rest is just the remainder
            hsmsPacket.Add((byte)(_message.DeviceId >> 8));
            hsmsPacket.Add((byte)(_message.DeviceId % 256));

            if ((SType)_message.SType == SType.Data)
            {
                var header3byte = _message.Stream;
                if (_message.Waitbit)
                    header3byte |= (1 << 7);
                hsmsPacket.Add((byte)header3byte);

                hsmsPacket.Add((byte)_message.Function);
            }
            else
            {
                hsmsPacket.Add((byte)0);
                hsmsPacket.Add((byte)0);
            }
            hsmsPacket.Add((byte)_message.PType);
            hsmsPacket.Add((byte)_message.SType);

            hsmsPacket.Add((byte)(_message.SystemBytes >> 24));
            hsmsPacket.Add((byte)(_message.SystemBytes >> 16));
            hsmsPacket.Add((byte)(_message.SystemBytes >> 8));
            hsmsPacket.Add((byte)(_message.SystemBytes % 256));

            return hsmsPacket.ToArray();
        }

    }
}
