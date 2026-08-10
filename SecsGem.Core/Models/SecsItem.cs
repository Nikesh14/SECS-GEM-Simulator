using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecsGem.Core.Models
{
    public abstract class SecsItem
    {
        public abstract SecsItemType ItemType { get; }
    }
     
    public sealed class AsciiItem : SecsItem
    {
        public AsciiItem(string value) 
        {
            Value = value;
        }
        public string Value { get; }
        public int Length => Value.Length;
        public override SecsItemType ItemType => SecsItemType.Ascii;
        
    }
    public sealed class ListItem : SecsItem
    {
        public ListItem(List<SecsItem> value)
        {
            Value = value;
        }
        public IReadOnlyList<SecsItem> Value { get; }
        public int Length => Value.Count;
        public override SecsItemType ItemType => SecsItemType.List;
    }
    public sealed class BinaryItem : SecsItem
    {
        public BinaryItem(List<byte> value) 
        {
            Value = value;
        }
        public IReadOnlyList<byte> Value { get; }
        public int Length => Value.Count;
        public override SecsItemType ItemType => SecsItemType.Binary;
    }
    public sealed class BooleanItem : SecsItem
    {
        public BooleanItem(List<bool> value)
        {
            Value = value;
        }
        public IReadOnlyList<bool> Value { get; }
        public int Length => Value.Count;
        public override SecsItemType ItemType => SecsItemType.Boolean;
    }
    public sealed class U1Item : SecsItem
    {
        public U1Item(List<byte> value) 
        {
            Value = value;
        }
        public IReadOnlyList<byte> Value { get; }
        public int Length => Value.Count;
        public override SecsItemType ItemType => SecsItemType.U1;
    }
    public sealed class U2Item : SecsItem
    {
        public U2Item(List<ushort> value)
        {
            Value = value;
        }
        public IReadOnlyList<ushort> Value { get; }
        public int Length => Value.Count;
        public override SecsItemType ItemType => SecsItemType.U2;
    }
    public sealed class U4Item : SecsItem
    {
        public U4Item(List<uint> value) 
        {
            Value = value;
        }
        public IReadOnlyList<uint> Value { get; }
        public int Length => Value.Count;
        public override SecsItemType ItemType => SecsItemType.U4;
    }
    public sealed class I1Item : SecsItem
    {
        public I1Item(List<sbyte> value)
        {
            Value = value;
        }
        public IReadOnlyList<sbyte> Value { get; }
        public int Length => Value.Count;
        public override SecsItemType ItemType => SecsItemType.I1;
    }
    public sealed class I2Item : SecsItem
    {
        public I2Item(List<short> value)
        {
            Value = value;
        }
        public IReadOnlyList<short> Value { get; }
        public int Length => Value.Count;
        public override SecsItemType ItemType => SecsItemType.I2;
    }
    public sealed class I4Item : SecsItem
    {
        public I4Item( List<int> value)
        {
            Value = value;
        }
        public IReadOnlyList<int> Value { get; }
        public int Length => Value.Count;
        public override SecsItemType ItemType => SecsItemType.I4;
    }
    public sealed class F4Item : SecsItem
    {
        public F4Item(List<float> value)
        {
            Value = value;
        }
        public IReadOnlyList<float> Value { get; }
        public int Length => Value.Count;
        public override SecsItemType ItemType => SecsItemType.F4;
    }
    public sealed class F8Item : SecsItem
    {
        public F8Item(List<double> value) 
        {
            Value = value;
        }
        public IReadOnlyList<double> Value { get; }
        public int Length => Value.Count;
        public override SecsItemType ItemType => SecsItemType.F8;
    }

    public sealed class JIS8Item : SecsItem
    {
        public JIS8Item(string value)
        {
            Value = value;
        }
        public string Value { get; }
        public int Length => Value.Length;
        public override SecsItemType ItemType => SecsItemType.JIS8;
    }
    public sealed class I8Item : SecsItem
    {
        public I8Item(List<long> value)
        {
            Value = value;
        }
        public List<long> Value { get; }
        public int Length => Value.Count;
        public override SecsItemType ItemType => SecsItemType.I8;
    }
    public sealed class U8Item : SecsItem
    {
        public U8Item(List<ulong> value)
        {
            Value = value;
        }
        public List<ulong> Value { get; }
        public int Length => Value.Count;
        public override SecsItemType ItemType => SecsItemType.U8;
    }

    public enum SecsItemType
    {
        List = 0,
        Ascii = 16,
        JIS8 = 17,
        Binary = 8,
        Boolean = 9,
        U1 = 41,
        U2 = 42,
        U4 = 44,
        U8 = 40,
        I1 = 25,
        I2 = 26,
        I8 = 24,
        I4 = 28,
        F4 = 36,
        F8 = 32
    }

    public class Utility
    {
        private readonly byte _headerByte;

        public Utility(byte headerByte)
        {
            _headerByte = headerByte;
        }
        public SecsItemType SecType ()
        {
            var typebyte = (_headerByte >> 2);
            if ( Enum.IsDefined(typeof(SecsItemType), typebyte))
            {
                return (SecsItemType)typebyte;
            }
            else
                throw new InvalidDataException($"Invalid SecsII message type {typebyte}");
        }
    }
}
