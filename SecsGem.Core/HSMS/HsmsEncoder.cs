using SecsGem.Core.Models;
using SecsGem.Core.SecIIMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecsGem.Core.HSMS
{
    public class HsmsEncoder
    {
        private readonly HsmsMessage _message;

        public HsmsEncoder(HsmsMessage message)
        {
            _message = message;
        }
        public byte[] Encode() 
        {
            List<byte> hsmsPacket = new List<byte>();
            // (highbyte)(256^1) + (lowbyte)(256^0) = deviceId
            // shifts the first 8 bytes to the right so the last 8 byte falls off giving us the high byte
            // and the rest is just the remainder
            hsmsPacket.Add((byte)(_message.DeviceId >> 8));
            hsmsPacket.Add((byte)(_message.DeviceId % 256));
            if (!Enum.IsDefined(typeof(SType), _message.SType))
            { 
                throw new InvalidDataException($"Invalid SType {_message.SType}");
            }
            if ((SType)_message.SType == SType.Data)
            {
                if (_message.Stream == null)
                    throw new InvalidOperationException($"You created an invalid HsmsMessage. A Data cannot have Stream as null.");
                //hsmsPacket.Add((byte)(_message.Stream ?? 0));
                if (_message.Stream > 127)
                    throw new InvalidOperationException($"Invalid function, it cannot be greater than 127!");
                var header3byte = _message.Stream;
                if(_message.Waitbit)
                {
                    header3byte |= (1 << 7);
                }
                hsmsPacket.Add((byte)header3byte);
                
                if (_message.Function == null)
                    throw new InvalidOperationException($"You created an invalid HsmsMessage. A Data cannot have Function as null.");
                hsmsPacket.Add((byte)_message.Function);
            }
            else
            {
                if (_message.Stream != null || _message.Function != null)
                    throw new InvalidOperationException($"You created an invalid HsmsMessage. A Select.req cannot contain Stream or Function.");
                hsmsPacket.Add((byte)0);
                hsmsPacket.Add((byte)0);
            }

            if(_message.PType == 0)
                hsmsPacket.Add((byte)_message.PType);
            else
                throw new InvalidOperationException($"You created an invalid HsmsMessage. PType cannot be greater than 0");
            hsmsPacket.Add((byte)_message.SType);

            hsmsPacket.Add((byte)(_message.SystemBytes >> 24));
            hsmsPacket.Add((byte)(_message.SystemBytes >> 16));
            hsmsPacket.Add((byte)(_message.SystemBytes >> 8));
            hsmsPacket.Add((byte)(_message.SystemBytes % 256));
            if (_message.Payload != null)
            {
                var encoder = new SecsIIEncoder(_message.Payload);
                hsmsPacket.AddRange(encoder.Encode());
            }

            var length = hsmsPacket.Count;
            var completePackage = new List<byte>();
            completePackage.Add((byte)(length >> 24));
            completePackage.Add((byte)(length >> 16));
            completePackage.Add((byte)(length >> 8));
            completePackage.Add((byte)(length % 256));
            completePackage.AddRange(hsmsPacket);

            return completePackage.ToArray();
        }

    }
}
