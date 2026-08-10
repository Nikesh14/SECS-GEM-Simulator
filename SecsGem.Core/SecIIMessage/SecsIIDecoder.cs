using SecsGem.Core.Models;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;


namespace SecsGem.Core.SecIIMessage
{
    public class SecsIIDecoder
    {
        private readonly byte[] _secs2Message;

        public SecsIIDecoder(byte[] SecsIIMessage)
        {
            if (SecsIIMessage == null)
                throw new ArgumentNullException(nameof(SecsIIMessage));
            _secs2Message = SecsIIMessage;
        }

        public DecodeInternal Decode()
        {
            if (_secs2Message.Length == 0)
                throw new InvalidDataException("Empty SecsII message");
            var secsItemType = GetItemType(_secs2Message[0]);
            // Extract the lower 2 bits (Length Byte Count - 1) and convert them to the actual length byte count.
            var lengthByteCount = (_secs2Message[0] & 0x03);
            if (lengthByteCount == 0)
                throw new InvalidDataException("Invalid SECS-II item: length-byte count cannot be 0.");
            if (_secs2Message.Length < 1 + lengthByteCount)
                throw new InvalidDataException("Incomplete SECS-II header. The message does not contain all required length bytes.");

            var payloadLength = CalculateValue<int>(_secs2Message.AsSpan((1), lengthByteCount).ToArray());

            if (_secs2Message.Length - 1 - lengthByteCount < payloadLength)
                throw new InvalidDataException("Incomplete SECS-II payload. The payload length exceeds the remaining message bytes.");
            var bytes = _secs2Message.AsSpan((lengthByteCount + 1), secsItemType != SecsItemType.List ? payloadLength : _secs2Message.Length - (lengthByteCount + 1));
            var byteConsumed = 1 + lengthByteCount + payloadLength;
            switch (secsItemType)
            {
                case SecsItemType.Ascii:
                    return new DecodeInternal(DecodeAsciiItem(bytes), byteConsumed) ;
                case SecsItemType.Binary:
                    return new DecodeInternal(DecodeBinaryItem(bytes), byteConsumed) ;
                case SecsItemType.Boolean:
                    return new DecodeInternal(DecodeBooleanItem(bytes), byteConsumed);
                case SecsItemType.U1:
                    return new DecodeInternal(DecodeU1Item(bytes), byteConsumed);
                case SecsItemType.U2:
                    return new DecodeInternal(DecodeU2Item(bytes), byteConsumed);
                case SecsItemType.U4:
                    return new DecodeInternal(DecodeU4Item(bytes), byteConsumed);
                case SecsItemType.U8:
                    return new DecodeInternal(DecodeU8Item(bytes), byteConsumed);
                case SecsItemType.I1:
                    return new DecodeInternal(DecodeI1Item(bytes), byteConsumed);
                case SecsItemType.I2:
                    return new DecodeInternal(DecodeI2Item(bytes), byteConsumed);
                case SecsItemType.I4:
                    return new DecodeInternal(DecodeI4Item(bytes), byteConsumed);
                case SecsItemType.I8:
                    return new DecodeInternal(DecodeI8Item(bytes), byteConsumed);
                case SecsItemType.F4:
                    return new DecodeInternal(DecodeF4Item(bytes), byteConsumed);
                case SecsItemType.F8:
                    return new DecodeInternal(DecodeF8Item(bytes), byteConsumed);
                case SecsItemType.List:
                    var item = DecodeListItem(bytes, payloadLength);
                    return new DecodeInternal(item.Item1, item.Item2 + 1 + lengthByteCount);
                default:
                    throw new NotSupportedException($"The SECS item type '{secsItemType}' is not supported by this decoder.");
            }
        }
        private AsciiItem DecodeAsciiItem(ReadOnlySpan<byte> bytes)
        {
            var asciiItem = new AsciiItem(Encoding.ASCII.GetString(bytes));
            return asciiItem;
        }
        private BinaryItem DecodeBinaryItem(ReadOnlySpan<byte> bytes)
        {
            var binaryBytes = new List<byte>();
            binaryBytes.AddRange(bytes);
            var binaryItem = new BinaryItem(binaryBytes);
            return binaryItem;
        }
        private BooleanItem DecodeBooleanItem(ReadOnlySpan<byte> bytes)
        {
            var booleanList = new List<bool>();
            foreach (byte b in bytes) 
            {
                if (b > 1)
                    throw new InvalidDataException("Invalid Boolean value. Expected 0 or 1.");
                if (b == 1)
                    booleanList.Add(true);
                else
                    booleanList.Add(false);
            }
            var booleanItem = new BooleanItem(booleanList);
            return booleanItem;
        }
        
        private U1Item DecodeU1Item(ReadOnlySpan<byte> bytes)
        {
            var u1Bytes = new List<byte>();
            u1Bytes.AddRange(bytes);
            var u1Item = new U1Item(u1Bytes);
            return u1Item;
        }
        private U2Item DecodeU2Item(ReadOnlySpan<byte> bytes)
        {
            var u2ItemList = new List<ushort>();
            int i = 0;
            if (bytes.Length % 2 == 0)
            {
                while (i < bytes.Length)
                {
                    u2ItemList.Add(CalculateValue<ushort>(bytes.Slice(i, 2)));
                    i += 2;
                }
            }
            else
            {
                throw new InvalidDataException("Invalid U2 payload length.\r\nExpected a multiple of 2 bytes.");
            }
            var u2Item = new U2Item(u2ItemList);
            return u2Item;
        }
        private U4Item DecodeU4Item(ReadOnlySpan<byte> bytes)
        {
            var u4ItemList = new List<uint>();
            int i = 0;
            if (bytes.Length % 4 == 0)
            {
                while (i < bytes.Length)
                {
                    u4ItemList.Add(CalculateValue<uint>(bytes.Slice(i, 4)));
                    i += 4;
                }
            }
            else
            {
                throw new InvalidDataException("Invalid U4 payload length.\r\nExpected a multiple of 4 bytes.");
            }
            var u4Item = new U4Item(u4ItemList);
            return u4Item;
        }
        private U8Item DecodeU8Item(ReadOnlySpan<byte> bytes)
        {
            var u8ItemList = new List<ulong>();
            int i = 0;
            if (bytes.Length % 8 == 0)
            {
                while (i < bytes.Length)
                {
                    u8ItemList.Add(CalculateValue<ulong>(bytes.Slice(i, 8)));
                    i += 8;
                }
            }
            else
            {
                throw new InvalidDataException("Invalid U8 payload length.\r\nExpected a multiple of 8 bytes.");
            }
            var u8Item = new U8Item(u8ItemList);
            return u8Item;
        }
        
        private I1Item DecodeI1Item(ReadOnlySpan<byte> bytes)
        {
            var i1Bytes = new List<sbyte>();
            foreach(byte b in bytes)
            {
                i1Bytes.Add(unchecked((sbyte)b));
            }
            var i1Item = new I1Item(i1Bytes);
            return i1Item;
        }
        private I2Item DecodeI2Item(ReadOnlySpan<byte> bytes)
        {
            var i2ItemList = new List<short>();
            int i = 0;
            if (bytes.Length % 2 == 0)
            {
                while (i < bytes.Length)
                {
                    i2ItemList.Add(CalculateValue<short>(bytes.Slice(i, 2)));
                    i += 2;
                }
            }
            else
            {
                throw new InvalidDataException("Invalid I2 payload length.\r\nExpected a multiple of 2 bytes.");
            }
            var i2Item = new I2Item(i2ItemList);
            return i2Item;
        }
        private I4Item DecodeI4Item(ReadOnlySpan<byte> bytes)
        {
            var i4ItemList = new List<int>();
            int i = 0;
            if (bytes.Length % 4 == 0)
            {
                while (i < bytes.Length)
                {
                    i4ItemList.Add(CalculateValue<int>(bytes.Slice(i, 4)));
                    i += 4;
                }
            }
            else
            {
                throw new InvalidDataException("Invalid I4 payload length.\r\nExpected a multiple of 4 bytes.");
            }
            var i4Item = new I4Item(i4ItemList);
            return i4Item;
        }
        private I8Item DecodeI8Item(ReadOnlySpan<byte> bytes)
        {
            var i8ItemList = new List<long>();
            int i = 0;
            if (bytes.Length % 8 == 0)
            {
                while (i < bytes.Length)
                {
                    i8ItemList.Add(CalculateValue<long>(bytes.Slice(i, 8)));
                    i += 8;
                }
            }
            else
            {
                throw new InvalidDataException("Invalid I8 payload length.\r\nExpected a multiple of 8 bytes.");
            }
            var i8Item = new I8Item(i8ItemList);
            return i8Item;
        }

        private F4Item DecodeF4Item(ReadOnlySpan<byte> bytes)
        {
            var f4ItemList = new List<float>();
            int i = 0;
            if (bytes.Length % 4 == 0)
            {
                while (i < bytes.Length)
                {
                    f4ItemList.Add(CalculateValue<float>(bytes.Slice(i, 4)));
                    i += 4;
                }
            }
            else
            {
                throw new InvalidDataException("Invalid F4 payload length.\r\nExpected a multiple of 4 bytes.");
            }
            var f4Item = new F4Item(f4ItemList);
            return f4Item;
        }
        private F8Item DecodeF8Item(ReadOnlySpan<byte> bytes)
        {
            var f8ItemList = new List<double>();
            int i = 0;
            if (bytes.Length % 8 == 0)
            {
                while (i < bytes.Length)
                {
                    f8ItemList.Add(CalculateValue<double>(bytes.Slice(i, 8)));
                    i += 8;
                }
            }
            else
            {
                throw new InvalidDataException("Invalid F8 payload length.\r\nExpected a multiple of 8 bytes.");
            }
            var f8Item = new F8Item(f8ItemList);
            return f8Item;
        }

        private (SecsItem, int) DecodeListItem(ReadOnlySpan<byte> bytes, int payloadLength)
        {
            int i = 0;
            var listData = new List<SecsItem>();
            while (payloadLength > 0)
            {
                if (i >= bytes.Length)
                    throw new InvalidDataException("Incomplete SECS-II List. Expected additional child items.");
                var decoder = new SecsIIDecoder(bytes.Slice(i, bytes.Length - i).ToArray());
                var decodedItem = decoder.Decode();
                listData.Add(decodedItem.SecsItem);
                i += decodedItem.BytesConsumed;
                --payloadLength;
            }
            var listDataitem = new ListItem(listData);
            return (listDataitem, i);
        }

        private SecsItemType GetItemType(byte _headerByte)
        {
            var typebyte = (_headerByte >> 2);
            if (Enum.IsDefined(typeof(SecsItemType), typebyte))
            {
                return (SecsItemType)typebyte;
            }
            else
                throw new InvalidDataException($"Invalid SecsII message type {typebyte}");
        }
        private T CalculateValue<T>(ReadOnlySpan<byte> bytes) where T : INumber<T>
        {
            if (typeof(T) == typeof(float))
            {
                float f = BinaryPrimitives.ReadSingleBigEndian(bytes); // Or ReadSingleLittleEndian
                return T.CreateChecked(f);
            }

            // Handle double (8 bytes)
            if (typeof(T) == typeof(double))
            {
                double d = BinaryPrimitives.ReadDoubleBigEndian(bytes); // Or ReadDoubleLittleEndian
                return T.CreateChecked(d);
            }
            T value = T.Zero;
            //T.CreateChecked converts a number to a new type,
            //but safely crashes your program with an error if the number is too big or too small to fit.
            T baseValue = T.CreateChecked(256);

            for (int i = 0; i < bytes.Length; ++i)
            {
                int power = bytes.Length - (i + 1);
                T multiplier = T.One;
                for (int p = 0; p < power; p++)
                {
                    multiplier *= baseValue;
                }
                value += T.CreateChecked(bytes[i]) * multiplier;
            }
            return value;
        }
    }

    public sealed class DecodeInternal
    {
        public DecodeInternal(SecsItem secsItem, int bytesCounsumed)
        {
            SecsItem = secsItem;
            BytesConsumed = bytesCounsumed;
        }
        public SecsItem? SecsItem { get; }
        public int BytesConsumed { get; }
    }
}
