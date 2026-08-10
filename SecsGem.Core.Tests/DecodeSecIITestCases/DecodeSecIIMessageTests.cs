using SecsGem.Core.Models;
using SecsGem.Core.SecIIMessage;

namespace SecsGem.Core.Tests.DecodeSecIITestCases
{
    /// <summary>
    /// GOLDEN-VECTOR decode tests. The input byte arrays are hand-verified, SEMI E5-conformant
    /// wire bytes (format byte low 2 bits = ACTUAL length-byte count, 1..3). They are NOT captured
    /// from our own encoder — feeding standard bytes is what proves the decoder reads the wire
    /// correctly, not just its own output.
    /// </summary>
    [TestClass]
    public sealed class DecodeSecIIMessageTests
    {
        [TestMethod]
        public void DecodeAscii()
        {
            byte[] input = [65, 3, 65, 66, 67];
            var item = new SecsIIDecoder(input).Decode();

            var ascii = item.SecsItem as AsciiItem;
            Assert.IsNotNull(ascii, "Expected an AsciiItem.");
            Assert.AreEqual(SecsItemType.Ascii, ascii.ItemType);
            Assert.AreEqual("ABC", ascii.Value);
        }

        [TestMethod]
        public void DecodeBinary()
        {
            byte[] input = [33, 3, 1, 2, 255];
            var item = new SecsIIDecoder(input).Decode();

            var binary = item.SecsItem as BinaryItem;
            Assert.IsNotNull(binary, "Expected a BinaryItem.");
            CollectionAssert.AreEqual(new byte[] { 1, 2, 255 }, binary.Value.ToArray());
        }

        [TestMethod]
        public void DecodeBoolean()
        {
            byte[] input = [37, 3, 1, 0, 1];
            var item = new SecsIIDecoder(input).Decode();

            var boolean = item.SecsItem as BooleanItem;
            Assert.IsNotNull(boolean, "Expected a BooleanItem.");
            CollectionAssert.AreEqual(new[] { true, false, true }, boolean.Value.ToArray());
        }

        [TestMethod]
        public void DecodeU1()
        {
            byte[] input = [165, 3, 1, 2, 3];
            var item = new SecsIIDecoder(input).Decode();

            var u1 = item.SecsItem as U1Item;
            Assert.IsNotNull(u1, "Expected a U1Item.");
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, u1.Value.ToArray());
        }

        [TestMethod]
        public void DecodeU2()
        {
            byte[] input = [169, 4, 0, 1, 1, 2];
            var item = new SecsIIDecoder(input).Decode();

            var u2 = item.SecsItem as U2Item;
            Assert.IsNotNull(u2, "Expected a U2Item.");
            CollectionAssert.AreEqual(new ushort[] { 1, 258 }, u2.Value.ToArray());
        }

        [TestMethod]
        public void DecodeU4()
        {
            byte[] input = [177, 4, 0, 0, 1, 2];
            var item = new SecsIIDecoder(input).Decode();

            var u4 = item.SecsItem as U4Item;
            Assert.IsNotNull(u4, "Expected a U4Item.");
            CollectionAssert.AreEqual(new uint[] { 258 }, u4.Value.ToArray());
        }

        [TestMethod]
        public void DecodeU8()
        {
            byte[] input = [161, 8, 0, 0, 0, 0, 0, 0, 1, 2];
            var item = new SecsIIDecoder(input).Decode();

            var u8 = item.SecsItem as U8Item;
            Assert.IsNotNull(u8, "Expected a U8Item.");
            CollectionAssert.AreEqual(new ulong[] { 258 }, u8.Value.ToArray());
        }

        [TestMethod]
        public void DecodeI1()
        {
            // 255 => -1 (two's complement)
            byte[] input = [101, 3, 255, 2, 3];
            var item = new SecsIIDecoder(input).Decode();

            var i1 = item.SecsItem as I1Item;
            Assert.IsNotNull(i1, "Expected an I1Item.");
            CollectionAssert.AreEqual(new sbyte[] { -1, 2, 3 }, i1.Value.ToArray());
        }

        [TestMethod]
        public void DecodeI2()
        {
            // FF FE => -2
            byte[] input = [105, 2, 255, 254];
            var item = new SecsIIDecoder(input).Decode();

            var i2 = item.SecsItem as I2Item;
            Assert.IsNotNull(i2, "Expected an I2Item.");
            CollectionAssert.AreEqual(new short[] { -2 }, i2.Value.ToArray());
        }

        [TestMethod]
        public void DecodeI4()
        {
            // FF FF FF FE => -2
            byte[] input = [113, 4, 255, 255, 255, 254];
            var item = new SecsIIDecoder(input).Decode();

            var i4 = item.SecsItem as I4Item;
            Assert.IsNotNull(i4, "Expected an I4Item.");
            CollectionAssert.AreEqual(new int[] { -2 }, i4.Value.ToArray());
        }

        [TestMethod]
        public void DecodeI8()
        {
            // FF x7 FE => -2
            byte[] input = [97, 8, 255, 255, 255, 255, 255, 255, 255, 254];
            var item = new SecsIIDecoder(input).Decode();

            var i8 = item.SecsItem as I8Item;
            Assert.IsNotNull(i8, "Expected an I8Item.");
            CollectionAssert.AreEqual(new long[] { -2 }, i8.Value.ToArray());
        }

        [TestMethod]
        public void DecodeF4()
        {
            // 0x3F800000 => 1.0f
            byte[] input = [145, 4, 63, 128, 0, 0];
            var item = new SecsIIDecoder(input).Decode();

            var f4 = item.SecsItem as F4Item;
            Assert.IsNotNull(f4, "Expected an F4Item.");
            CollectionAssert.AreEqual(new float[] { 1.0f }, f4.Value.ToArray());
        }

        [TestMethod]
        public void DecodeF8()
        {
            // 0x3FF0000000000000 => 1.0
            byte[] input = [129, 8, 63, 240, 0, 0, 0, 0, 0, 0];
            var item = new SecsIIDecoder(input).Decode();

            var f8 = item.SecsItem as F8Item;
            Assert.IsNotNull(f8, "Expected an F8Item.");
            CollectionAssert.AreEqual(new double[] { 1.0 }, f8.Value.ToArray());
        }

        [TestMethod]
        public void DecodeList()
        {
            // List [ Ascii "A", U1 { 1 } ]
            byte[] input = [1, 2, 65, 1, 65, 165, 1, 1];
            var item = new SecsIIDecoder(input).Decode();

            var list = item.SecsItem as ListItem;
            Assert.IsNotNull(list, "Expected a ListItem.");
            Assert.AreEqual(2, list.Value.Count);

            var child0 = list.Value[0] as AsciiItem;
            Assert.IsNotNull(child0, "First child should be an AsciiItem.");
            Assert.AreEqual("A", child0.Value);

            var child1 = list.Value[1] as U1Item;
            Assert.IsNotNull(child1, "Second child should be a U1Item.");
            CollectionAssert.AreEqual(new byte[] { 1 }, child1.Value.ToArray());
        }

        // ---------- Edge cases ----------

        [TestMethod]
        public void DecodeAsciiEmpty()
        {
            byte[] input = [65, 0];
            var item = new SecsIIDecoder(input).Decode();

            var ascii = item.SecsItem as AsciiItem;
            Assert.IsNotNull(ascii, "Expected an AsciiItem.");
            Assert.AreEqual("", ascii.Value);
        }

        [TestMethod]
        public void DecodeListEmpty()
        {
            byte[] input = [1, 0];
            var item = new SecsIIDecoder(input).Decode();

            var list = item.SecsItem as ListItem;
            Assert.IsNotNull(list, "Expected a ListItem.");
            Assert.AreEqual(0, list.Value.Count);
        }

        [TestMethod]
        public void DecodeAsciiTwoLengthBytes()
        {
            // format byte 66 => low 2 bits = 2 => two length bytes; 0x0100 = 256
            var input = new List<byte> { 66, 1, 0 };
            input.AddRange(Enumerable.Repeat((byte)65, 256)); // 256 'A'
            var item = new SecsIIDecoder(input.ToArray()).Decode();

            var ascii = item.SecsItem as AsciiItem;
            Assert.IsNotNull(ascii, "Expected an AsciiItem.");
            Assert.AreEqual(256, ascii.Value.Length);
            Assert.AreEqual(new string('A', 256), ascii.Value);
        }

        [TestMethod]
        public void DecodeNestedList()
        {
            // List [ List [ Ascii "A" ] ]
            byte[] input = [1, 1, 1, 1, 65, 1, 65];
            var item = new SecsIIDecoder(input).Decode();

            var outer = item.SecsItem as ListItem;
            Assert.IsNotNull(outer, "Expected an outer ListItem.");
            Assert.AreEqual(1, outer.Value.Count);

            var inner = outer.Value[0] as ListItem;
            Assert.IsNotNull(inner, "Expected an inner ListItem.");
            Assert.AreEqual(1, inner.Value.Count);

            var leaf = inner.Value[0] as AsciiItem;
            Assert.IsNotNull(leaf, "Expected an AsciiItem leaf.");
            Assert.AreEqual("A", leaf.Value);
        }

        [TestMethod]
        public void DecodeInvalidItemTypeThrows()
        {
            // format code 0b111111 (63) is not a defined SecsItemType.
            byte[] input = [0b1111_1100, 0];
            Assert.ThrowsExactly<InvalidDataException>(() => new SecsIIDecoder(input).Decode());
        }

        [TestMethod]
        public void DecodeBooleanInvalidValueThrows()
        {
            // Boolean bytes must be 0 or 1; 2 is invalid.  (format (9<<2)|1 = 37, length 1, value 2)
            byte[] input = [37, 1, 2];
            Assert.ThrowsExactly<InvalidDataException>(() => new SecsIIDecoder(input).Decode());
        }

        [TestMethod]
        public void DecodeU2MisalignedLengthThrows()
        {
            // U2 payload must be a multiple of 2 bytes; a single byte is invalid.  (format (42<<2)|1 = 169)
            byte[] input = [169, 1, 5];
            Assert.ThrowsExactly<InvalidDataException>(() => new SecsIIDecoder(input).Decode());
        }

        // ---------- Nested list structure ----------

        [TestMethod]
        public void DecodeNestedListTwoLevels()
        {
            // <L <L <A "ABC">> <U1 1>>
            //   outer: 1,2
            //     inner list: 1,1
            //       ascii "ABC": 65,3,65,66,67
            //     u1 {1}: 165,1,1
            byte[] input = [1, 2, 1, 1, 65, 3, 65, 66, 67, 165, 1, 1];
            var item = new SecsIIDecoder(input).Decode();

            var outer = item.SecsItem as ListItem;
            Assert.IsNotNull(outer, "Expected an outer ListItem.");
            Assert.AreEqual(2, outer.Value.Count);

            var inner = outer.Value[0] as ListItem;
            Assert.IsNotNull(inner, "First child should be a ListItem.");
            Assert.AreEqual("ABC", ((AsciiItem)inner.Value[0]).Value);

            var u1 = outer.Value[1] as U1Item;
            Assert.IsNotNull(u1, "Second child should be a U1Item.");
            CollectionAssert.AreEqual(new byte[] { 1 }, u1.Value.ToArray());
        }

        [TestMethod]
        public void DecodeNestedListThreeLevels()
        {
            // <L <L <L <A "ABC">>>>
            byte[] input = [1, 1, 1, 1, 1, 1, 65, 3, 65, 66, 67];
            var item = new SecsIIDecoder(input).Decode();

            var l0 = item.SecsItem as ListItem;
            Assert.IsNotNull(l0);
            var l1 = l0.Value[0] as ListItem;
            Assert.IsNotNull(l1);
            var l2 = l1.Value[0] as ListItem;
            Assert.IsNotNull(l2);
            var leaf = l2.Value[0] as AsciiItem;
            Assert.IsNotNull(leaf);
            Assert.AreEqual("ABC", leaf.Value);
        }

        [TestMethod]
        public void DecodeEmptyNestedList()
        {
            // <L <L> <A "ABC">>
            //   outer: 1,2
            //     empty inner list: 1,0
            //     ascii "ABC": 65,3,65,66,67
            byte[] input = [1, 2, 1, 0, 65, 3, 65, 66, 67];
            var item = new SecsIIDecoder(input).Decode();

            var outer = item.SecsItem as ListItem;
            Assert.IsNotNull(outer);
            Assert.AreEqual(2, outer.Value.Count);

            var empty = outer.Value[0] as ListItem;
            Assert.IsNotNull(empty, "First child should be an (empty) ListItem.");
            Assert.AreEqual(0, empty.Value.Count);
            Assert.AreEqual("ABC", ((AsciiItem)outer.Value[1]).Value);
        }

        // ---------- Malformed / truncated input ----------
        // The decoder must reject these rather than return garbage. It currently surfaces a mix of
        // exception types (InvalidDataException for its own validation, but ArgumentOutOfRange /
        // IndexOutOfRange from span slicing on truncated buffers), so these assert only that *some*
        // exception is thrown. See notes if you want the decoder to normalize these to InvalidDataException.

        private static void AssertThrowsAny(Action action)
        {
            try
            {
                action();
            }
            catch
            {
                return;
            }
            Assert.Fail("Expected an exception, but none was thrown.");
        }

        [TestMethod]
        public void DecodeInvalidFormatByte255Throws()
        {
            // 255 >> 2 = 63, which is not a defined SecsItemType.
            byte[] input = [255];
            Assert.ThrowsExactly<InvalidDataException>(() => new SecsIIDecoder(input).Decode());
        }

        [TestMethod]
        public void DecodeLengthExceedsBufferThrows()
        {
            // ASCII, length says 5, but only 1 payload byte is present.
            byte[] input = [65, 5, 65];
            AssertThrowsAny(() => new SecsIIDecoder(input).Decode());
        }

        [TestMethod]
        public void DecodeTruncatedU4Throws()
        {
            // U4, length says 4, but only 2 payload bytes are present.
            byte[] input = [177, 4, 0, 0];
            AssertThrowsAny(() => new SecsIIDecoder(input).Decode());
        }

        [TestMethod]
        public void DecodeTruncatedListThrows()
        {
            // List says 2 children, but only one complete child is present.
            byte[] input = [1, 2, 65, 1, 65];
            AssertThrowsAny(() => new SecsIIDecoder(input).Decode());
        }
    }
}
