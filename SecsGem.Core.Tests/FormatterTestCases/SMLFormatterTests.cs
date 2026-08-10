using SecsGem.Core.Formatter;
using SecsGem.Core.Models;

namespace SecsGem.Core.Tests.FormatterTestCases
{
    /// <summary>
    /// Tests for <see cref="SMLFormatter"/> — the human-readable SECS Message Language
    /// renderer. The formatter takes an <see cref="HsmsMessage"/> and produces a header line
    /// (SxFy [W]) followed by the payload item tree. These assert the per-item-type output.
    /// </summary>
    [TestClass]
    public sealed class SMLFormatterTests
    {
        // Render a Data message carrying a single payload item.
        private static string Format(SecsItem payload, byte stream = 1, byte function = 2, bool wbit = false)
            => new SMLFormatter(new HsmsMessage
            {
                DeviceId = 1,
                SType = SType.Data,
                Stream = stream,
                Function = function,
                Waitbit = wbit,
                Payload = payload
            }).FormatMessage();

        [TestMethod]
        public void Header_IncludesStreamFunction_AndWBitOnlyWhenSet()
        {
            var withWait = new SMLFormatter(new HsmsMessage
            { SType = SType.Data, Stream = 1, Function = 13, Waitbit = true }).FormatMessage();
            var noWait = new SMLFormatter(new HsmsMessage
            { SType = SType.Data, Stream = 1, Function = 2, Waitbit = false }).FormatMessage();

            StringAssert.StartsWith(withWait, "S1F13 W");
            StringAssert.StartsWith(noWait, "S1F2");
            Assert.IsFalse(noWait.Contains(" W"), "A non-wait message must not show the W-bit.");
        }

        [TestMethod]
        public void Ascii_IsQuoted()
        {
            StringAssert.Contains(Format(new AsciiItem("MODEL-X")), "<A \"MODEL-X\">");
        }

        [TestMethod]
        public void Binary_IsRenderedInHex()
        {
            // 0x00, 0x01, 0x85 -> "0 1 85"
            var sml = Format(new BinaryItem(new List<byte> { 0x00, 0x01, 0x85 }));
            StringAssert.Contains(sml, "<B 0 1 85>");
        }

        [TestMethod]
        public void Boolean_RendersValues_NotTypeName()
        {
            // Regression guard: previously interpolated the list object and printed its type name.
            var sml = Format(new BooleanItem(new List<bool> { true, false }));
            StringAssert.Contains(sml, "<BOOLEAN True False>");
        }

        [TestMethod]
        public void UnsignedInteger_RendersDecimalValues()
        {
            StringAssert.Contains(Format(new U4Item(new List<uint> { 42, 7 })), "<U4 42 7>");
        }

        [TestMethod]
        public void SignedInteger_RendersNegativeValues()
        {
            StringAssert.Contains(Format(new I2Item(new List<short> { -5 })), "<I2 -5>");
        }

        [TestMethod]
        public void EmptyList_RendersAsL0()
        {
            StringAssert.Contains(Format(new ListItem(new List<SecsItem>())), "<L[0]>");
        }

        [TestMethod]
        public void NestedList_RendersOuterCountAndAllChildren()
        {
            var payload = new ListItem(new List<SecsItem>
            {
                new AsciiItem("A"),
                new U1Item(new List<byte> { 1 })
            });

            var sml = Format(payload);

            StringAssert.Contains(sml, "<L[2]");
            StringAssert.Contains(sml, "<A \"A\">");
            StringAssert.Contains(sml, "<U1 1>");
        }
    }
}
