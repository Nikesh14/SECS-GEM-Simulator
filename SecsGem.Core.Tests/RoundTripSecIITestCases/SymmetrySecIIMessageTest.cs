using SecsGem.Core.Models;
using SecsGem.Core.SecIIMessage;

namespace SecsGem.Core.Tests.RoundTripSecIITestCases
{
    /// <summary>
    /// Decode → Encode symmetry: start from a raw byte array, decode it, re-encode the result,
    /// and assert the bytes come back identical. This is the mirror of the Encode → Decode
    /// round-trip and catches encoder bugs that a value-only comparison would miss.
    /// Inputs are SEMI E5-conformant golden vectors (format byte low 2 bits = actual length-byte count).
    /// </summary>
    [TestClass]
    public sealed class SymmetrySecIIMessageTests
    {
        private static void AssertDecodeEncodeSymmetry(byte[] input)
        {
            var decoded = new SecsIIDecoder(input).Decode().SecsItem!;
            var reEncoded = new SecsIIEncoder(decoded).Encode();
            CollectionAssert.AreEqual(input, reEncoded, "Decode → Encode did not reproduce the original bytes.");
        }

        [TestMethod]
        public void SymmetryAscii()
        {
            AssertDecodeEncodeSymmetry([65, 3, 65, 66, 67]);
        }

        [TestMethod]
        public void SymmetryU2()
        {
            AssertDecodeEncodeSymmetry([169, 4, 0, 1, 1, 2]);
        }

        [TestMethod]
        public void SymmetryI8()
        {
            AssertDecodeEncodeSymmetry([97, 8, 255, 255, 255, 255, 255, 255, 255, 254]);
        }

        [TestMethod]
        public void SymmetryF8()
        {
            AssertDecodeEncodeSymmetry([129, 8, 63, 240, 0, 0, 0, 0, 0, 0]);
        }

        [TestMethod]
        public void SymmetryBoolean()
        {
            AssertDecodeEncodeSymmetry([37, 3, 1, 0, 1]);
        }

        [TestMethod]
        public void SymmetryList()
        {
            // <L <A "A"> <U1 1>>
            AssertDecodeEncodeSymmetry([1, 2, 65, 1, 65, 165, 1, 1]);
        }

        [TestMethod]
        public void SymmetryNestedList()
        {
            // <L <L <A "ABC">> <U1 1>>
            AssertDecodeEncodeSymmetry([1, 2, 1, 1, 65, 3, 65, 66, 67, 165, 1, 1]);
        }

        [TestMethod]
        public void SymmetryEmptyNestedList()
        {
            // <L <L> <A "ABC">>
            AssertDecodeEncodeSymmetry([1, 2, 1, 0, 65, 3, 65, 66, 67]);
        }
    }
}
