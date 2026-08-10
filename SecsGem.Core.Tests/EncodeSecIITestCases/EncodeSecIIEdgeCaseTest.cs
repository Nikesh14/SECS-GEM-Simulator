using SecsGem.Core.Models;
using SecsGem.Core.SecIIMessage;
using System.Text;

namespace SecsGem.Core.Tests.EncodeSecIITestCases
{
    /// <summary>
    /// GOLDEN-VECTOR edge cases (see <see cref="EncodeSecIIMessageTests"/> for the convention):
    /// empty payloads, the 1/2/3 length-byte boundaries, list recursion, multi-element numerics,
    /// and the sign bit of a floating point value. Expected bytes are hand-verified against SEMI E5.
    /// </summary>
    [TestClass]
    public sealed class EncodeSecIIEdgeCaseTests
    {
        // ---------- Empty payloads (length field == 0) ----------
        // An empty item still carries ONE length byte of 0x00, so the format byte's low 2 bits = 1.
        // e.g. empty ASCII => (16<<2)|1 = 65, then 0x00.

        [TestMethod]
        public void EncodeAsciiEmpty()
        {
            var encodedBytes = new SecsIIEncoder(new AsciiItem("")).Encode();

            byte[] expectedBytes = [65, 0];
            CollectionAssert.AreEqual(expectedBytes, encodedBytes, "Empty ASCII should encode to a header + one zero length byte.");
        }

        [TestMethod]
        public void EncodeBinaryEmpty()
        {
            var encodedBytes = new SecsIIEncoder(new BinaryItem(new List<byte>())).Encode();

            byte[] expectedBytes = [33, 0];
            CollectionAssert.AreEqual(expectedBytes, encodedBytes, "Empty Binary should encode to a header + one zero length byte.");
        }

        [TestMethod]
        public void EncodeU4Empty()
        {
            // Empty numeric: EncodePrimitiveDatabytes returns 0 bytes, so length == 0.
            var encodedBytes = new SecsIIEncoder(new U4Item(new List<uint>())).Encode();

            byte[] expectedBytes = [177, 0];
            CollectionAssert.AreEqual(expectedBytes, encodedBytes, "Empty U4 should encode to a header + one zero length byte.");
        }

        [TestMethod]
        public void EncodeListEmpty()
        {
            var encodedBytes = new SecsIIEncoder(new ListItem(new List<SecsItem>())).Encode();

            byte[] expectedBytes = [1, 0];   // (0<<2)|1, element count 0
            CollectionAssert.AreEqual(expectedBytes, encodedBytes, "Empty List should encode to a header + zero element count.");
        }

        // ---------- Length-byte-count boundaries ----------
        // The number of length bytes is packed into the low 2 bits of the format byte as the
        // ACTUAL count (1..3). All happy-path tests use payloads < 256 (1 length byte); these cross over.

        [TestMethod]
        public void EncodeAsciiLength255()
        {
            // 255 bytes is the largest payload that still fits in ONE length byte.
            var value = new string('A', 255);
            var encodedBytes = new SecsIIEncoder(new AsciiItem(value)).Encode();

            var expected = new List<byte> { 65, 255 };           // format (16<<2)|1, length 255
            expected.AddRange(Encoding.ASCII.GetBytes(value));

            Assert.AreEqual(expected.Count, encodedBytes.Count, "Length 255 should use exactly one length byte.");
            CollectionAssert.AreEqual(expected, encodedBytes);
        }

        [TestMethod]
        public void EncodeAsciiLength256()
        {
            // 256 crosses into TWO length bytes: format byte low bits become 2, length = 0x01 0x00.
            var value = new string('A', 256);
            var encodedBytes = new SecsIIEncoder(new AsciiItem(value)).Encode();

            var expected = new List<byte> { 66, 1, 0 };          // format (16<<2)|2, length 0x0100 = 256
            expected.AddRange(Encoding.ASCII.GetBytes(value));

            Assert.AreEqual(expected.Count, encodedBytes.Count, "Length 256 should use two length bytes.");
            CollectionAssert.AreEqual(expected, encodedBytes);
        }

        [TestMethod]
        public void EncodeAsciiLength65536()
        {
            // 65536 crosses into THREE length bytes: format byte low bits become 3, length = 0x01 0x00 0x00.
            var value = new string('A', 65536);
            var encodedBytes = new SecsIIEncoder(new AsciiItem(value)).Encode();

            var expected = new List<byte> { 67, 1, 0, 0 };       // format (16<<2)|3, length 0x010000 = 65536
            expected.AddRange(Encoding.ASCII.GetBytes(value));

            Assert.AreEqual(expected.Count, encodedBytes.Count, "Length 65536 should use three length bytes.");
            CollectionAssert.AreEqual(expected, encodedBytes);
        }

        // ---------- List recursion ----------

        [TestMethod]
        public void EncodeNestedList()
        {
            // List [ List [ Ascii "A" ] ]
            //   outer list header : 1, 1   (1 element)
            //   inner list header : 1, 1   (1 element)
            //   ascii "A"         : 65, 1, 65
            var sec2Item = new ListItem(new List<SecsItem>
            {
                new ListItem(new List<SecsItem> { new AsciiItem("A") })
            });
            var encodedBytes = new SecsIIEncoder(sec2Item).Encode();

            byte[] expectedBytes = [1, 1, 1, 1, 65, 1, 65];
            CollectionAssert.AreEqual(expectedBytes, encodedBytes, "Nested list did not recurse correctly.");
        }

        // ---------- Multi-element numerics ----------

        [TestMethod]
        public void EncodeF4MultipleElements()
        {
            // Two floats => 8 payload bytes, each big-endian IEEE-754 single.
            //   1.0f => 0x3F800000 => 63,128,0,0
            //   2.0f => 0x40000000 => 64,0,0,0
            var sec2Item = new F4Item(new List<float> { 1.0f, 2.0f });
            var encodedBytes = new SecsIIEncoder(sec2Item).Encode();

            byte[] expectedBytes = [145, 8, 63, 128, 0, 0, 64, 0, 0, 0];   // (36<<2)|1
            CollectionAssert.AreEqual(expectedBytes, encodedBytes, "Multi-element F4 payload/length is wrong.");
        }

        [TestMethod]
        public void EncodeF8Negative()
        {
            // -1.0 double => 0xBFF0000000000000 (sign bit set in the leading byte after big-endian reversal).
            var sec2Item = new F8Item(new List<double> { -1.0 });
            var encodedBytes = new SecsIIEncoder(sec2Item).Encode();

            byte[] expectedBytes = [129, 8, 191, 240, 0, 0, 0, 0, 0, 0];   // (32<<2)|1
            CollectionAssert.AreEqual(expectedBytes, encodedBytes, "Negative F8 sign bit / byte order is wrong.");
        }

        // ---------- Invalid encoder input ----------

        [TestMethod]
        public void EncodeNullItemThrows()
        {
            // Encode() guards against a null item.
            var encoder = new SecsIIEncoder(null!);
            Assert.ThrowsExactly<ArgumentNullException>(() => encoder.Encode());
        }
    }
}
