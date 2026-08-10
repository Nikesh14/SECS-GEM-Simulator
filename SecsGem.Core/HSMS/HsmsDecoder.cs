using SecsGem.Core.Models;
using SecsGem.Core.SecIIMessage;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SecsGem.Core.HSMS
{
    public class HsmsDecoder
    {
        private readonly byte[] _packet;

        public HsmsDecoder(byte[] packet)
        {
            _packet = packet;
        }

        public Models.HsmsMessage Decode()
        {
            var hsmsMessage = new Models.HsmsMessage();

            if (_packet.Length >= 10)
            {
                var deviceIdBytes = CalculateValue<UInt16>(_packet.AsSpan(0, 2).ToArray());
                var header3Byte = _packet[2];
                var header4Byte = _packet[3];
                var pTypeByte = _packet[4];
                var sTypeByte = _packet[5];
                var systemBytes = CalculateValue<UInt32>(_packet.AsSpan(6, 4).ToArray());

                var payloadBytes = _packet.AsSpan(10, _packet.Length - 10);

                
                hsmsMessage.DeviceId = deviceIdBytes;
                if (Enum.IsDefined(typeof(SType), (int)sTypeByte))
                {
                    hsmsMessage.SType = (SType)sTypeByte;
                }
                else
                {
                    throw new InvalidDataException($"Invalid SType {sTypeByte}");
                }
                hsmsMessage.SystemBytes = systemBytes;

                if (sTypeByte == 0)
                {
                    var temp = CalculateFunction(header3Byte);
                    hsmsMessage.Waitbit = temp.Item2;
                    hsmsMessage.Stream = temp.Item1;
                    hsmsMessage.Function = header4Byte;
                }

                if (pTypeByte != 0) hsmsMessage.PType = pTypeByte;

                if (payloadBytes.Length > 0)
                {
                    var decoder = new SecsIIDecoder(payloadBytes.ToArray());

                    hsmsMessage.Payload = decoder.Decode().SecsItem;
                }
            }
            else
            {
                throw new Exception("Insufficient Packet data!");
            }
            return hsmsMessage;
        }


        private T CalculateValue<T>(byte[] bytes) where T : INumber<T> 
        {
            T value = T.Zero;
            //T.CreateChecked converts a number to a new type,
            //but safely crashes your program with an error if the number is too big or too small to fit.
            T baseValue = T.CreateChecked(256);

            for (int i = 0; i < bytes.Length; ++i)
            {
                int power = bytes.Length - (i + 1);
                T multiplier = T.One;
                for(int p=0; p<power; p++)
                {
                    multiplier *= baseValue;
                }
                value += T.CreateChecked(bytes[i]) * multiplier;
            }
            return value;
        }

        private (byte, bool) CalculateFunction(byte streambyte)
        {
            bool wbit = streambyte >= 128 ? true : false;
            
            if(wbit)
            {
                streambyte ^= (1 << 7);
            }
            return (streambyte, wbit);
        }
    }
}
