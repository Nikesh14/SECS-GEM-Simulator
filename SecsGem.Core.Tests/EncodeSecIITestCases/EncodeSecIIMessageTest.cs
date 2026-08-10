using SecsGem.Core.Models;
using SecsGem.Core.SecIIMessage;
using System.Text;

namespace SecsGem.Core.Tests.EncodeSecIITestCases
{
    /// <summary>
    /// GOLDEN-VECTOR TESTS. Each expected byte array is a hand-verified, SEMI E5-conformant
    /// encoding — NOT a value captured from our own encoder. These pin the wire format so a
    /// regression (e.g. a swapped W-bit or an off-by-one length-byte count) fails loudly here,
    /// which a round-trip test alone would not catch. Do not "fix" a failure by copying the
    /// actual bytes; verify against the standard first.
    ///
    /// Format byte = (formatCode &lt;&lt; 2) | numberOfLengthBytes, where numberOfLengthBytes is
    /// the ACTUAL count (1..3). e.g. ASCII (code 16): 1 length byte => (16&lt;&lt;2)|1 = 65 (0x41).
    /// </summary>
    [TestClass]
    public sealed class EncodeSecIIMessageTests
    {
        [TestMethod]
        public void EncodeAscii()
        {
            var sec2Item = new AsciiItem("ABC");
            var encoder = new SecsIIEncoder(sec2Item);
            var encodedBytes = encoder.Encode();

            byte[] expectedBytes = [65, 3, 65, 66, 67];   // (16<<2)|1, len 3, 'A','B','C'
            Assert.IsTrue(encodedBytes != null, "TestFailed encoded byte is null.");
            Assert.AreEqual(encodedBytes.Count, expectedBytes.Length, "TestFailed encoded byte count not same.");
            CollectionAssert.AreEqual(expectedBytes, encodedBytes, "TestFaild Encoded and Expected byte not matching");
        }

        [TestMethod]
        public void EncodeBinary()
        {
            var sec2Item = new BinaryItem(new List<byte> { 1, 2, 255 });
            var encoder = new SecsIIEncoder(sec2Item);
            var encodedBytes = encoder.Encode();

            byte[] expectedBytes = [33, 3, 1, 2, 255];    // (8<<2)|1
            Assert.IsTrue(encodedBytes != null, "TestFailed encoded byte is null.");
            Assert.AreEqual(encodedBytes.Count, expectedBytes.Length, "TestFailed encoded byte count not same.");
            CollectionAssert.AreEqual(expectedBytes, encodedBytes, "TestFaild Encoded and Expected byte not matching");
        }

        [TestMethod]
        public void EncodeBoolean()
        {
            var sec2Item = new BooleanItem(new List<bool> { true, false, true });
            var encoder = new SecsIIEncoder(sec2Item);
            var encodedBytes = encoder.Encode();

            byte[] expectedBytes = [37, 3, 1, 0, 1];      // (9<<2)|1
            Assert.IsTrue(encodedBytes != null, "TestFailed encoded byte is null.");
            Assert.AreEqual(encodedBytes.Count, expectedBytes.Length, "TestFailed encoded byte count not same.");
            CollectionAssert.AreEqual(expectedBytes, encodedBytes, "TestFaild Encoded and Expected byte not matching");
        }

        [TestMethod]
        public void EncodeU1()
        {
            var sec2Item = new U1Item(new List<byte> { 1, 2, 3 });
            var encoder = new SecsIIEncoder(sec2Item);
            var encodedBytes = encoder.Encode();

            byte[] expectedBytes = [165, 3, 1, 2, 3];     // (41<<2)|1
            Assert.IsTrue(encodedBytes != null, "TestFailed encoded byte is null.");
            Assert.AreEqual(encodedBytes.Count, expectedBytes.Length, "TestFailed encoded byte count not same.");
            CollectionAssert.AreEqual(expectedBytes, encodedBytes, "TestFaild Encoded and Expected byte not matching");
        }

        [TestMethod]
        public void EncodeU2()
        {
            // Two elements => 4 payload bytes, big-endian: 1 => 00 01, 258 => 01 02
            var sec2Item = new U2Item(new List<ushort> { 1, 258 });
            var encoder = new SecsIIEncoder(sec2Item);
            var encodedBytes = encoder.Encode();

            byte[] expectedBytes = [169, 4, 0, 1, 1, 2];  // (42<<2)|1
            Assert.IsTrue(encodedBytes != null, "TestFailed encoded byte is null.");
            Assert.AreEqual(encodedBytes.Count, expectedBytes.Length, "TestFailed encoded byte count not same.");
            CollectionAssert.AreEqual(expectedBytes, encodedBytes, "TestFaild Encoded and Expected byte not matching");
        }

        [TestMethod]
        public void EncodeU4()
        {
            // 258 => 00 00 01 02 (big-endian)
            var sec2Item = new U4Item(new List<uint> { 258 });
            var encoder = new SecsIIEncoder(sec2Item);
            var encodedBytes = encoder.Encode();

            byte[] expectedBytes = [177, 4, 0, 0, 1, 2];  // (44<<2)|1
            Assert.IsTrue(encodedBytes != null, "TestFailed encoded byte is null.");
            Assert.AreEqual(encodedBytes.Count, expectedBytes.Length, "TestFailed encoded byte count not same.");
            CollectionAssert.AreEqual(expectedBytes, encodedBytes, "TestFaild Encoded and Expected byte not matching");
        }

        [TestMethod]
        public void EncodeU8()
        {
            // 258 => 00 00 00 00 00 00 01 02 (big-endian)
            var sec2Item = new U8Item(new List<ulong> { 258 });
            var encoder = new SecsIIEncoder(sec2Item);
            var encodedBytes = encoder.Encode();

            byte[] expectedBytes = [161, 8, 0, 0, 0, 0, 0, 0, 1, 2];   // (40<<2)|1
            Assert.IsTrue(encodedBytes != null, "TestFailed encoded byte is null.");
            Assert.AreEqual(encodedBytes.Count, expectedBytes.Length, "TestFailed encoded byte count not same.");
            CollectionAssert.AreEqual(expectedBytes, encodedBytes, "TestFaild Encoded and Expected byte not matching");
        }

        [TestMethod]
        public void EncodeI1()
        {
            // -1 => 255 (two's complement, single byte)
            var sec2Item = new I1Item(new List<sbyte> { -1, 2, 3 });
            var encoder = new SecsIIEncoder(sec2Item);
            var encodedBytes = encoder.Encode();

            byte[] expectedBytes = [101, 3, 255, 2, 3];   // (25<<2)|1
            Assert.IsTrue(encodedBytes != null, "TestFailed encoded byte is null.");
            Assert.AreEqual(encodedBytes.Count, expectedBytes.Length, "TestFailed encoded byte count not same.");
            CollectionAssert.AreEqual(expectedBytes, encodedBytes, "TestFaild Encoded and Expected byte not matching");
        }

        [TestMethod]
        public void EncodeI2()
        {
            // -2 => FF FE (big-endian, two's complement)
            var sec2Item = new I2Item(new List<short> { -2 });
            var encoder = new SecsIIEncoder(sec2Item);
            var encodedBytes = encoder.Encode();

            byte[] expectedBytes = [105, 2, 255, 254];    // (26<<2)|1
            Assert.IsTrue(encodedBytes != null, "TestFailed encoded byte is null.");
            Assert.AreEqual(encodedBytes.Count, expectedBytes.Length, "TestFailed encoded byte count not same.");
            CollectionAssert.AreEqual(expectedBytes, encodedBytes, "TestFaild Encoded and Expected byte not matching");
        }

        [TestMethod]
        public void EncodeI4()
        {
            // -2 => FF FF FF FE (big-endian, two's complement)
            var sec2Item = new I4Item(new List<int> { -2 });
            var encoder = new SecsIIEncoder(sec2Item);
            var encodedBytes = encoder.Encode();

            byte[] expectedBytes = [113, 4, 255, 255, 255, 254];   // (28<<2)|1
            Assert.IsTrue(encodedBytes != null, "TestFailed encoded byte is null.");
            Assert.AreEqual(encodedBytes.Count, expectedBytes.Length, "TestFailed encoded byte count not same.");
            CollectionAssert.AreEqual(expectedBytes, encodedBytes, "TestFaild Encoded and Expected byte not matching");
        }

        [TestMethod]
        public void EncodeI8()
        {
            // -2 => FF FF FF FF FF FF FF FE (big-endian, two's complement)
            var sec2Item = new I8Item(new List<long> { -2 });
            var encoder = new SecsIIEncoder(sec2Item);
            var encodedBytes = encoder.Encode();

            byte[] expectedBytes = [97, 8, 255, 255, 255, 255, 255, 255, 255, 254];   // (24<<2)|1
            Assert.IsTrue(encodedBytes != null, "TestFailed encoded byte is null.");
            Assert.AreEqual(encodedBytes.Count, expectedBytes.Length, "TestFailed encoded byte count not same.");
            CollectionAssert.AreEqual(expectedBytes, encodedBytes, "TestFaild Encoded and Expected byte not matching");
        }

        [TestMethod]
        public void EncodeF4()
        {
            // 1.0f => IEEE-754 single 0x3F800000 (big-endian)
            var sec2Item = new F4Item(new List<float> { 1.0f });
            var encoder = new SecsIIEncoder(sec2Item);
            var encodedBytes = encoder.Encode();

            byte[] expectedBytes = [145, 4, 63, 128, 0, 0];   // (36<<2)|1
            Assert.IsTrue(encodedBytes != null, "TestFailed encoded byte is null.");
            Assert.AreEqual(encodedBytes.Count, expectedBytes.Length, "TestFailed encoded byte count not same.");
            CollectionAssert.AreEqual(expectedBytes, encodedBytes, "TestFaild Encoded and Expected byte not matching");
        }

        [TestMethod]
        public void EncodeF8()
        {
            // 1.0 => IEEE-754 double 0x3FF0000000000000 (big-endian)
            var sec2Item = new F8Item(new List<double> { 1.0 });
            var encoder = new SecsIIEncoder(sec2Item);
            var encodedBytes = encoder.Encode();

            byte[] expectedBytes = [129, 8, 63, 240, 0, 0, 0, 0, 0, 0];   // (32<<2)|1
            Assert.IsTrue(encodedBytes != null, "TestFailed encoded byte is null.");
            Assert.AreEqual(encodedBytes.Count, expectedBytes.Length, "TestFailed encoded byte count not same.");
            CollectionAssert.AreEqual(expectedBytes, encodedBytes, "TestFaild Encoded and Expected byte not matching");
        }

        [TestMethod]
        public void EncodeList()
        {
            // List length field is the ELEMENT count (2), not byte count.
            //   child 1: Ascii "A"  => 65, 1, 65
            //   child 2: U1 { 1 }   => 165, 1, 1
            var sec2Item = new ListItem(new List<SecsItem>
            {
                new AsciiItem("A"),
                new U1Item(new List<byte> { 1 })
            });
            var encoder = new SecsIIEncoder(sec2Item);
            var encodedBytes = encoder.Encode();

            byte[] expectedBytes = [1, 2, 65, 1, 65, 165, 1, 1];   // list (0<<2)|1, then children
            Assert.IsTrue(encodedBytes != null, "TestFailed encoded byte is null.");
            Assert.AreEqual(encodedBytes.Count, expectedBytes.Length, "TestFailed encoded byte count not same.");
            CollectionAssert.AreEqual(expectedBytes, encodedBytes, "TestFaild Encoded and Expected byte not matching");
        }
    }
}
