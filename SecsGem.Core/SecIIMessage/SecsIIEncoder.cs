using SecsGem.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SecsGem.Core.SecIIMessage
{
    public class SecsIIEncoder
    {
        private readonly SecsItem _secsItem;

        public SecsIIEncoder(SecsItem secsItem)
        {
            _secsItem = secsItem;
        }

        public List<byte>Encode()
        {
            if (_secsItem == null)
                throw new ArgumentNullException(nameof(_secsItem));
            var encodedSec2Message = new List<byte>();

            switch(_secsItem.ItemType)
            {
                case SecsItemType.Ascii:
                    var asciiItem = (AsciiItem)_secsItem;
                    encodedSec2Message.AddRange(EncodeFormatBytes(asciiItem.Length, SecsItemType.Ascii));
                    encodedSec2Message.AddRange(Encoding.ASCII.GetBytes(asciiItem.Value));
                    break;
                case SecsItemType.JIS8:
                    // TODO: Implement JIS-8 encoding.
                    // Currently encoded as ASCII.
                    var jis8Item = (JIS8Item)_secsItem;
                    encodedSec2Message.AddRange(EncodeFormatBytes(jis8Item.Length, SecsItemType.JIS8));
                    encodedSec2Message.AddRange(Encoding.ASCII.GetBytes(jis8Item.Value));
                    break;
                case SecsItemType.Binary:
                    var binaryItem = (BinaryItem)_secsItem;
                    encodedSec2Message.AddRange(EncodeFormatBytes(binaryItem.Length, SecsItemType.Binary));
                    encodedSec2Message.AddRange(binaryItem.Value);
                    break;
                case SecsItemType.Boolean:
                    var booleanItem = (BooleanItem)_secsItem;
                    encodedSec2Message.AddRange(EncodeFormatBytes(booleanItem.Length, SecsItemType.Boolean));
                    for(int i=0; i<booleanItem.Value.Count; i++)
                        encodedSec2Message.Add((byte)(booleanItem.Value[i] ? 1 : 0 ));
                    break;
                case SecsItemType.U1:
                    var u1Item = (U1Item)_secsItem;
                    encodedSec2Message.AddRange(EncodeFormatBytes(u1Item.Length, SecsItemType.U1));
                    encodedSec2Message.AddRange(u1Item.Value);
                    break;
                case SecsItemType.U2:
                    var u2Item = (U2Item)_secsItem;
                    var encodedU2PayloadByte = EncodePrimitiveDatabytes(u2Item.Value);
                    encodedSec2Message.AddRange(EncodeFormatBytes(encodedU2PayloadByte.Count, SecsItemType.U2));
                    encodedSec2Message.AddRange(encodedU2PayloadByte);
                    break;
                case SecsItemType.U4:
                    var u4Item = (U4Item)_secsItem;
                    var encodedU4PayloadByte = EncodePrimitiveDatabytes(u4Item.Value);
                    encodedSec2Message.AddRange(EncodeFormatBytes(encodedU4PayloadByte.Count, SecsItemType.U4));
                    encodedSec2Message.AddRange(encodedU4PayloadByte);
                    break;
                case SecsItemType.U8:
                    var u8Item = (U8Item)_secsItem;
                    var encodedU8PayloadByte = EncodePrimitiveDatabytes(u8Item.Value);
                    encodedSec2Message.AddRange(EncodeFormatBytes(encodedU8PayloadByte.Count, SecsItemType.U8));
                    encodedSec2Message.AddRange(encodedU8PayloadByte);
                    break;
                case SecsItemType.I1:
                    var i1Item = (I1Item)_secsItem;
                    encodedSec2Message.AddRange(EncodeFormatBytes(i1Item.Length, SecsItemType.I1));
                    encodedSec2Message.AddRange(EncodePrimitiveDatabytes(i1Item.Value));
                    break;
                case SecsItemType.I2:
                    var i2Item = (I2Item)_secsItem;
                    var encodedI2PayloadByte = EncodePrimitiveDatabytes(i2Item.Value);
                    encodedSec2Message.AddRange(EncodeFormatBytes(encodedI2PayloadByte.Count, SecsItemType.I2));
                    encodedSec2Message.AddRange(encodedI2PayloadByte);
                    break;
                case SecsItemType.I4:
                    var i4Item = (I4Item)_secsItem;
                    var encodedI4PayloadByte = EncodePrimitiveDatabytes(i4Item.Value);
                    encodedSec2Message.AddRange(EncodeFormatBytes(encodedI4PayloadByte.Count, SecsItemType.I4));
                    encodedSec2Message.AddRange(encodedI4PayloadByte);
                    break;
                case SecsItemType.I8:
                    var i8Item = (I8Item)_secsItem;
                    var encodedI8PayloadByte = EncodePrimitiveDatabytes(i8Item.Value);
                    encodedSec2Message.AddRange(EncodeFormatBytes(encodedI8PayloadByte.Count, SecsItemType.I8));
                    encodedSec2Message.AddRange(encodedI8PayloadByte);
                    break;
                case SecsItemType.F4:
                    var f4Item = (F4Item)_secsItem;
                    var encodedF4PayloadByte = EncodePrimitiveDatabytes(f4Item.Value);
                    encodedSec2Message.AddRange(EncodeFormatBytes(encodedF4PayloadByte.Count, SecsItemType.F4));
                    encodedSec2Message.AddRange(encodedF4PayloadByte);
                    break;
                case SecsItemType.F8:
                    var f8Item = (F8Item)_secsItem;
                    var encodedF8PayloadByte = EncodePrimitiveDatabytes(f8Item.Value);
                    encodedSec2Message.AddRange(EncodeFormatBytes(encodedF8PayloadByte.Count, SecsItemType.F8));
                    encodedSec2Message.AddRange(encodedF8PayloadByte);
                    break;
                case SecsItemType.List:
                    var listItem = (ListItem)_secsItem;
                    encodedSec2Message.AddRange(EncodeFormatBytes(listItem.Length, SecsItemType.List));
                    encodedSec2Message.AddRange(EncodeListDatabytes(listItem.Value));
                    break;
            }
            return encodedSec2Message;
        }

        private List<byte>EncodeFormatBytes(int length, SecsItemType type)
        {
            var formatByte = new List<byte>();
            var lengthByte = new Stack<byte>();
            while(length > 0)
            {
                lengthByte.Push((byte)(length % 256));
                length /= 256;
            }
            if(lengthByte.Count > 3)
                throw new OverflowException("Item length exceeds the maximum supported SECS-II length.");
            var headerByte = ((int)type << 2) | ((lengthByte.Count) == 0 ? 1 : lengthByte.Count);

            formatByte.Add((byte)headerByte);
            formatByte.AddRange(lengthByte.Count == 0 ? [0] : lengthByte.ToList());
            return formatByte;
        }

        private List<byte>EncodePrimitiveDatabytes<T>(IReadOnlyList<T> items)
        {
            var dataBytes = new List<byte>();

            foreach (var item in items)
            {
                byte[] objectBytes = item switch
                {
                    sbyte sb => new[] { unchecked((byte) sb)}, 
                    float f => BitConverter.GetBytes(f),
                    double d => BitConverter.GetBytes(d),
                    short s => BitConverter.GetBytes(s),
                    ushort us => BitConverter.GetBytes(us),
                    int i => BitConverter.GetBytes(i),
                    uint ui => BitConverter.GetBytes(ui),
                    long l => BitConverter.GetBytes(l),
                    ulong ul => BitConverter.GetBytes(ul),

                    _ => throw new NotSupportedException($"Type {typeof(T)} is not supported.")
                };
                if (objectBytes.Length > 1 && BitConverter.IsLittleEndian)
                    Array.Reverse(objectBytes);

                dataBytes.AddRange(objectBytes);
            }

            return dataBytes;
        }

        private List<byte>EncodeListDatabytes(IReadOnlyList<SecsItem> items)
        {
            var list = new List<byte>();
            foreach(SecsItem item in items)
            {
                var encoder = new SecsIIEncoder(item);
                list.AddRange(encoder.Encode());
            }
            return list;
        }

    }
}
