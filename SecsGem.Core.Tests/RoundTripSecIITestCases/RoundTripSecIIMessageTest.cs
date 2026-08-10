using SecsGem.Core.Models;
using SecsGem.Core.SecIIMessage;

namespace SecsGem.Core.Tests.RoundTripSecIITestCases
{
    /// <summary>
    /// Round-trip tests: encode an item, decode the bytes back, and assert the value survives.
    /// These exercise encoder and decoder together and use boundary values (min/max/zero/negative)
    /// that would be tedious to hand-encode.
    /// </summary>
    [TestClass]
    public sealed class RoundTripSecIIMessageTests
    {
        private static SecsItem RoundTrip(SecsItem item)
        {
            var bytes = new SecsIIEncoder(item).Encode();
            return new SecsIIDecoder(bytes.ToArray()).Decode().SecsItem!;
        }

        [TestMethod]
        public void RoundTripAscii()
        {
            var value = "Hello, SECS-II! 0123";
            var decoded = RoundTrip(new AsciiItem(value)) as AsciiItem;
            Assert.IsNotNull(decoded);
            Assert.AreEqual(value, decoded.Value);
        }

        [TestMethod]
        public void RoundTripAsciiEmpty()
        {
            var decoded = RoundTrip(new AsciiItem("")) as AsciiItem;
            Assert.IsNotNull(decoded);
            Assert.AreEqual("", decoded.Value);
        }

        [TestMethod]
        public void RoundTripBinary()
        {
            var value = new List<byte> { 0, 1, 127, 128, 255 };
            var decoded = RoundTrip(new BinaryItem(value)) as BinaryItem;
            Assert.IsNotNull(decoded);
            CollectionAssert.AreEqual(value, decoded.Value.ToArray());
        }

        [TestMethod]
        public void RoundTripBoolean()
        {
            var value = new List<bool> { true, false, true, true, false };
            var decoded = RoundTrip(new BooleanItem(value)) as BooleanItem;
            Assert.IsNotNull(decoded);
            CollectionAssert.AreEqual(value, decoded.Value.ToArray());
        }

        [TestMethod]
        public void RoundTripU1()
        {
            var value = new List<byte> { 0, 1, 128, 255 };
            var decoded = RoundTrip(new U1Item(value)) as U1Item;
            Assert.IsNotNull(decoded);
            CollectionAssert.AreEqual(value, decoded.Value.ToArray());
        }

        [TestMethod]
        public void RoundTripU2()
        {
            var value = new List<ushort> { 0, 1, 258, ushort.MaxValue };
            var decoded = RoundTrip(new U2Item(value)) as U2Item;
            Assert.IsNotNull(decoded);
            CollectionAssert.AreEqual(value, decoded.Value.ToArray());
        }

        [TestMethod]
        public void RoundTripU4()
        {
            var value = new List<uint> { 0, 1, 258, uint.MaxValue };
            var decoded = RoundTrip(new U4Item(value)) as U4Item;
            Assert.IsNotNull(decoded);
            CollectionAssert.AreEqual(value, decoded.Value.ToArray());
        }

        [TestMethod]
        public void RoundTripU8()
        {
            var value = new List<ulong> { 0, 1, 258, ulong.MaxValue };
            var decoded = RoundTrip(new U8Item(value)) as U8Item;
            Assert.IsNotNull(decoded);
            CollectionAssert.AreEqual(value, decoded.Value.ToArray());
        }

        [TestMethod]
        public void RoundTripI1()
        {
            var value = new List<sbyte> { sbyte.MinValue, -1, 0, 1, sbyte.MaxValue };
            var decoded = RoundTrip(new I1Item(value)) as I1Item;
            Assert.IsNotNull(decoded);
            CollectionAssert.AreEqual(value, decoded.Value.ToArray());
        }

        [TestMethod]
        public void RoundTripI2()
        {
            var value = new List<short> { short.MinValue, -2, -1, 0, 1, short.MaxValue };
            var decoded = RoundTrip(new I2Item(value)) as I2Item;
            Assert.IsNotNull(decoded);
            CollectionAssert.AreEqual(value, decoded.Value.ToArray());
        }

        [TestMethod]
        public void RoundTripI4()
        {
            var value = new List<int> { int.MinValue, -2, -1, 0, 1, int.MaxValue };
            var decoded = RoundTrip(new I4Item(value)) as I4Item;
            Assert.IsNotNull(decoded);
            CollectionAssert.AreEqual(value, decoded.Value.ToArray());
        }

        [TestMethod]
        public void RoundTripI8()
        {
            var value = new List<long> { long.MinValue, -2, -1, 0, 1, long.MaxValue };
            var decoded = RoundTrip(new I8Item(value)) as I8Item;
            Assert.IsNotNull(decoded);
            CollectionAssert.AreEqual(value, decoded.Value.ToArray());
        }

        [TestMethod]
        public void RoundTripF4()
        {
            var value = new List<float> { 0f, 1f, -1f, 3.14159f, float.MinValue, float.MaxValue };
            var decoded = RoundTrip(new F4Item(value)) as F4Item;
            Assert.IsNotNull(decoded);
            CollectionAssert.AreEqual(value, decoded.Value.ToArray());
        }

        [TestMethod]
        public void RoundTripF8()
        {
            var value = new List<double> { 0d, 1d, -1d, Math.PI, double.MinValue, double.MaxValue };
            var decoded = RoundTrip(new F8Item(value)) as F8Item;
            Assert.IsNotNull(decoded);
            CollectionAssert.AreEqual(value, decoded.Value.ToArray());
        }

        [TestMethod]
        public void RoundTripList()
        {
            // List [ Ascii "AB", U2 { 7 }, List [ Ascii "C" ] ]
            var original = new ListItem(new List<SecsItem>
            {
                new AsciiItem("AB"),
                new U2Item(new List<ushort> { 7 }),
                new ListItem(new List<SecsItem> { new AsciiItem("C") })
            });

            var decoded = RoundTrip(original) as ListItem;
            Assert.IsNotNull(decoded);
            Assert.AreEqual(3, decoded.Value.Count);
            Assert.AreEqual("AB", ((AsciiItem)decoded.Value[0]).Value);
            CollectionAssert.AreEqual(new ushort[] { 7 }, ((U2Item)decoded.Value[1]).Value.ToArray());
            var inner = (ListItem)decoded.Value[2];
            Assert.AreEqual("C", ((AsciiItem)inner.Value[0]).Value);
        }

        [TestMethod]
        public void RoundTripListTwoLevels()
        {
            // <L <L <A "ABC">> <U1 1>>
            var original = new ListItem(new List<SecsItem>
            {
                new ListItem(new List<SecsItem> { new AsciiItem("ABC") }),
                new U1Item(new List<byte> { 1 })
            });

            var decoded = RoundTrip(original) as ListItem;
            Assert.IsNotNull(decoded);
            Assert.AreEqual(2, decoded.Value.Count);

            var inner = decoded.Value[0] as ListItem;
            Assert.IsNotNull(inner);
            Assert.AreEqual("ABC", ((AsciiItem)inner.Value[0]).Value);
            CollectionAssert.AreEqual(new byte[] { 1 }, ((U1Item)decoded.Value[1]).Value.ToArray());
        }

        [TestMethod]
        public void RoundTripListThreeLevels()
        {
            // <L <L <L <A "ABC">>>>
            var original = new ListItem(new List<SecsItem>
            {
                new ListItem(new List<SecsItem>
                {
                    new ListItem(new List<SecsItem> { new AsciiItem("ABC") })
                })
            });

            var decoded = RoundTrip(original) as ListItem;
            Assert.IsNotNull(decoded);
            var l1 = decoded.Value[0] as ListItem;
            Assert.IsNotNull(l1);
            var l2 = l1.Value[0] as ListItem;
            Assert.IsNotNull(l2);
            var leaf = l2.Value[0] as AsciiItem;
            Assert.IsNotNull(leaf);
            Assert.AreEqual("ABC", leaf.Value);
        }

        [TestMethod]
        public void RoundTripEmptyNestedList()
        {
            // <L <L> <A "ABC">>  — an empty list as the first child, then a scalar.
            var original = new ListItem(new List<SecsItem>
            {
                new ListItem(new List<SecsItem>()),
                new AsciiItem("ABC")
            });

            var decoded = RoundTrip(original) as ListItem;
            Assert.IsNotNull(decoded);
            Assert.AreEqual(2, decoded.Value.Count);

            var empty = decoded.Value[0] as ListItem;
            Assert.IsNotNull(empty);
            Assert.AreEqual(0, empty.Value.Count);
            Assert.AreEqual("ABC", ((AsciiItem)decoded.Value[1]).Value);
        }

        [TestMethod]
        public void RoundTripMixedList()
        {
            // <L <A> <Binary> <Boolean> <U2> <I8> <F8>> — one of every shape in a single list.
            var original = new ListItem(new List<SecsItem>
            {
                new AsciiItem("XY"),
                new BinaryItem(new List<byte> { 1, 255 }),
                new BooleanItem(new List<bool> { true, false }),
                new U2Item(new List<ushort> { 300 }),
                new I8Item(new List<long> { -5 }),
                new F8Item(new List<double> { 2.5 })
            });

            var decoded = RoundTrip(original) as ListItem;
            Assert.IsNotNull(decoded);
            Assert.AreEqual(6, decoded.Value.Count);
            Assert.AreEqual("XY", ((AsciiItem)decoded.Value[0]).Value);
            CollectionAssert.AreEqual(new byte[] { 1, 255 }, ((BinaryItem)decoded.Value[1]).Value.ToArray());
            CollectionAssert.AreEqual(new[] { true, false }, ((BooleanItem)decoded.Value[2]).Value.ToArray());
            CollectionAssert.AreEqual(new ushort[] { 300 }, ((U2Item)decoded.Value[3]).Value.ToArray());
            CollectionAssert.AreEqual(new long[] { -5 }, ((I8Item)decoded.Value[4]).Value.ToArray());
            CollectionAssert.AreEqual(new double[] { 2.5 }, ((F8Item)decoded.Value[5]).Value.ToArray());
        }

        [TestMethod]
        public void RoundTripLargeList()
        {
            // 100 children — proves the recursive decoder advances by exactly the bytes each child consumes.
            var children = new List<SecsItem>();
            for (int i = 0; i < 100; i++)
                children.Add(new U2Item(new List<ushort> { (ushort)i }));
            var original = new ListItem(children);

            var decoded = RoundTrip(original) as ListItem;
            Assert.IsNotNull(decoded);
            Assert.AreEqual(100, decoded.Value.Count);
            for (int i = 0; i < 100; i++)
                CollectionAssert.AreEqual(new ushort[] { (ushort)i }, ((U2Item)decoded.Value[i]).Value.ToArray());
        }
    }
}
