using SecsGem.Core.Models;
using SecsGem.Core.SecIIMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecsGem.Core.Formatter
{
    public class SMLFormatter
    {
        private readonly HsmsMessage _message;

        public SMLFormatter(HsmsMessage message)
        {
            _message = message;
        }
        public string FormatMessage()
        {
            string SML = $"S{_message.Stream}F{_message.Function}" + (_message.Waitbit ? " W." : "");
            string SecItem = "";
            if (_message.Payload != null)
            {
                SecItem = FormatSecsItem(_message.Payload!);
            }
            return SML + '\n' +'\n' +SecItem;
        }

        private string FormatSecsItem(SecsItem message, int indentLevel = 0)
        {
            if (message is AsciiItem)
            {
                return $@"<A ""{((AsciiItem)message!).Value}"">";
            }
            else if (message is BinaryItem)
            {
                var hexBinaryItem = HexConverter(((BinaryItem)message!).Value);
                return $"<B {GenerateSML<string>(hexBinaryItem)}>";
            }
            else if (message is BooleanItem)
            {
                return $"<BOOLEAN {GenerateSML<bool>(((BooleanItem)message!).Value)}>";
            }
            else if (message is U1Item)
            {
                return $"<U1 {GenerateSML<byte>(((U1Item)message!).Value)}>";
            }
            else if (message is U2Item)
            {
                return $"<U2 {GenerateSML<ushort>(((U2Item)message!).Value)}>";
            }
            else if (message is U4Item)
            {
                return $"<U4 {GenerateSML<uint>(((U4Item)message!).Value)}>";
            }
            else if (message is U8Item)
            {
                return $"<U8 {GenerateSML<ulong>(((U8Item)message!).Value)}>";
            }
            else if (message is I1Item)
            {
                return $"<I1 {GenerateSML<sbyte>(((I1Item)message!).Value)}>";
            }
            else if (message is I2Item)
            {
                return $"<I2 {GenerateSML<short>(((I2Item)message!).Value)}>";
            }
            else if (message is I4Item)
            {
                return $"<I4 {GenerateSML<int>(((I4Item)message!).Value)}>";
            }
            else if (message is I8Item)
            {
                return $"<I8 {GenerateSML<long>(((I8Item)message!).Value)}>";
            }
            else if (message is F4Item)
            {
                return $"<F4 {GenerateSML<float>(((F4Item)message!).Value)}>";
            }
            else if (message is F8Item)
            {
                return $"<F8 {GenerateSML<double>(((F8Item)message!).Value)}>";
            }
            else if (message is ListItem)
            {
                var sml = new StringBuilder(); ;
                var childIndentLevel = ++indentLevel;
                foreach (var element in ((ListItem)message!).Value)
                {
                    sml.Append($"{GetIndent(indentLevel)}{FormatSecsItem(element, childIndentLevel)}\n");
                }
                if (sml.Length == 0) return "<L[0]>";
                else return $"""
                        <L[{((ListItem)message!).Value.Count}]
                        {sml}
                        {GetIndent(childIndentLevel - 1)}>
                        """;
            }
            else throw new InvalidDataException("Unidentified Format!");
        }
        private string GenerateSML<T>(IEnumerable<T> values)
        {
            var sml = new StringBuilder() ;
            foreach (var element in values)
            {
                sml.Append($"{element.ToString()} ");
            }
            return sml.ToString().TrimEnd(' ');
        }
        private string GetIndent(int multiplier)
        {
            // Multiplies the depth by 4 to get the total number of spaces
            return new string(' ', multiplier * 4);
        }

        private List<string> HexConverter(IReadOnlyList<byte> decValue)
        {
            var hexConverted = new List<string>(); 
            
            foreach (var dec in decValue) 
            {
                if (dec == 0)
                {
                    hexConverted.Add("0");
                    continue;
                }
                Stack<char> hexValue = new Stack<char>();
                var decimalValue = dec;
                while(decimalValue > 0)
                {
                    int remainder = (decimalValue % 16);
                    var hexChar = remainder > 9 ? (char)(65 + remainder % 10) : (char)(48 + remainder);
                    hexValue.Push(hexChar);
                    decimalValue /= 16;
                }
                hexConverted.Add(new string(hexValue.ToArray()));
            }
            return hexConverted;
        }
    }
}
