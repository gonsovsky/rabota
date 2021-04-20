using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Csud.Base
{
    public enum CamlFilterOperation
    {
        And,
        Or
    }

    public class CamlFilter
    {
        public CamlFilter()
        {
            Where = CamlNode.MakeUnary(CamlNodeType.Where, null);
        }

        public CamlFilter(CamlNode node)
        {
            Where = node.NodeType == CamlNodeType.Where
                ? (UnaryCamlNode)node
                : CamlNode.MakeUnary(CamlNodeType.Where, node);
        }

        public UnaryCamlNode Where { get; private set; }

        public int Take { get; set; }

        public int Skip { get; set; }

        public static CamlFilter Empty => new CamlFilter();

        public static implicit operator CamlFilter(CamlNode node)
        {
            return new CamlFilter(node);
        }

        public static CamlFilter Parse(XElement xml)
        {
            var parser = new CamlNodeParser();
            return parser.Parse(xml);
        }

        public static CamlFilter Parse(string xml)
        {
            return Parse(XElement.Parse(xml));
        }

        public override string ToString()
        {
            return Where.ToString();
        }


        public CamlFilter Merge(CamlFilterOperation op, CamlFilter filter)
        {
            if (Where.Node == null || filter.Where.Node == null)
            {
                return new CamlFilter
                {
                    Where = CamlNode.MakeUnary(CamlNodeType.Where, Where.Node ?? filter.Where.Node)
                };
            }

            switch (op)
            {
                case CamlFilterOperation.And:
                    return new CamlFilter
                    {
                        Where = CamlNode.MakeUnary(CamlNodeType.Where,
                            CamlNode.MakeBinary(CamlNodeType.And, Where.Node, filter.Where.Node))
                    };
                case CamlFilterOperation.Or:
                    return new CamlFilter
                    {
                        Where = CamlNode.MakeUnary(CamlNodeType.Where,
                            CamlNode.MakeBinary(CamlNodeType.Or, Where.Node, filter.Where.Node))
                    };
                default:
                    throw new NotImplementedException();
            }
        }

        #region Factory methods

        public static BinaryCamlNode Eq(FieldRefCamlNode fieldRef, ValueCamlNode valueNode)
        {
            return CamlNode.MakeBinary(CamlNodeType.Eq, fieldRef, valueNode);
        }

        public static BinaryCamlNode Neq(FieldRefCamlNode fieldRef, ValueCamlNode valueNode)
        {
            return CamlNode.MakeBinary(CamlNodeType.Neq, fieldRef, valueNode);
        }

        public static BinaryCamlNode Contains(FieldRefCamlNode fieldRef, ValueCamlNode valueNode)
        {
            return CamlNode.MakeBinary(CamlNodeType.Contains, fieldRef, valueNode);
        }

        public static BinaryCamlNode In(FieldRefCamlNode fieldRef, MultipleValueCamlNode valueNode)
        {
            return CamlNode.MakeBinary(CamlNodeType.In, fieldRef, valueNode);
        }

        public static UnaryCamlNode IsNull(FieldRefCamlNode fieldRef)
        {
            return CamlNode.MakeUnary(CamlNodeType.IsNull, fieldRef);
        }

        public static UnaryCamlNode IsNotNull(FieldRefCamlNode fieldRef)
        {
            return CamlNode.MakeUnary(CamlNodeType.IsNotNull, fieldRef);
        }

        #endregion
    }

    public abstract class CamlNode
    {
        protected CamlNode(CamlNodeType nodeType)
        {
            NodeType = nodeType;
        }

        public CamlNodeType NodeType { get; }

        #region Factory methods

        public static UnaryCamlNode MakeUnary(CamlNodeType nodeType, CamlNode node)
        {
            return new UnaryCamlNode(nodeType, node);
        }

        public static BinaryCamlNode MakeBinary(CamlNodeType nodeType, CamlNode left, CamlNode right)
        {
            return new BinaryCamlNode(nodeType, left, right);
        }

        public static FieldRefCamlNode MakeField(string fieldName)
        {
            return new FieldRefCamlNode(fieldName);
        }

        public static ValueCamlNode MakeValue(ValueCamlNode.Type valueType, string valueString)
        {
            return new ValueCamlNode(valueType, valueString);
        }

        public static MultipleValueCamlNode MakeMultipleValue(MultipleValueCamlNode.Type valueType,
            string[] valueString)
        {
            return new MultipleValueCamlNode(valueType, valueString);
        }

        public static ValueCamlNode MakeValue<T>(T value)
        {
            var valueType = ValueCamlNode.Type.Text;
            var valueString = value.ToString();

            if (typeof(T) == typeof(DateTime))
            {
                valueType = ValueCamlNode.Type.DateTime;
            }
            else if (typeof(T) == typeof(int))
            {
                valueType = ValueCamlNode.Type.Integer;
            }
            return new ValueCamlNode(valueType, valueString);
        }

        #endregion
    }


    public class BinaryCamlNode : CamlNode
    {
        public BinaryCamlNode(CamlNodeType nodeType, CamlNode left, CamlNode right)
            : base(nodeType)
        {
            Left = left ?? throw new ArgumentNullException(nameof(left));
            Right = right ?? throw new ArgumentNullException(nameof(right));
        }

        public CamlNode Left { get; }

        public CamlNode Right { get; }

        public override string ToString()
        {
            return string.Format("<{0}>{1}{2}</{0}>", NodeType, Left, Right);
        }
    }


    public class UnaryCamlNode : CamlNode
    {
        public UnaryCamlNode(CamlNodeType nodeType, CamlNode node)
            : base(nodeType)
        {
            Node = node;
        }

        public CamlNode Node { get; }

        public override string ToString()
        {
            return string.Format("<{0}>{1}</{0}>", NodeType, Node?.ToString() ?? string.Empty);
        }
    }


    public class FieldRefCamlNode : CamlNode
    {
        public FieldRefCamlNode(string fieldName) : base(CamlNodeType.FieldRef)
        {
            Name = fieldName;
        }

        public string Name { get; }


        public static implicit operator FieldRefCamlNode(string fieldName)
        {
            return new FieldRefCamlNode(fieldName);
        }

        public override string ToString()
        {
            return $"<{NodeType} Name='{Name}'/>";
        }
    }


    public class ValueCamlNode : CamlNode
    {
        public enum Type
        {
            Text,
            Integer,
            DateTime,
            Binary,
            Parameter,
            System
        }

        public ValueCamlNode(Type valueType, string value) : base(CamlNodeType.Value)
        {
            ValueType = valueType;
            Value = value;
        }

        public Type ValueType { get; }

        public string Value { get; }


        public static implicit operator ValueCamlNode(string value)
        {
            return new ValueCamlNode(Type.Text, value);
        }

        public static implicit operator ValueCamlNode(Guid value)
        {
            return new ValueCamlNode(Type.Text, value.ToString());
        }

        public static implicit operator ValueCamlNode(bool value)
        {
            return new ValueCamlNode(Type.Integer, Convert.ToInt32(value).ToString(CultureInfo.InvariantCulture));
        }

        public static implicit operator ValueCamlNode(int value)
        {
            return new ValueCamlNode(Type.Integer, value.ToString(CultureInfo.InvariantCulture));
        }

        public static implicit operator ValueCamlNode(long value)
        {
            return new ValueCamlNode(Type.Integer, value.ToString(CultureInfo.InvariantCulture));
        }

        public static ValueCamlNode FromObject(object value)
        {
            if (value.IsIntegerType())
                return new ValueCamlNode(Type.Integer, value.ToString());
            switch (value)
            {
                case DateTime time:
                    return new ValueCamlNode(Type.DateTime, time.Serialize());
                case bool _:
                    return new ValueCamlNode(Type.Integer,
                        Convert.ToInt32(value).ToString(CultureInfo.InvariantCulture));
                case byte[] binaryValue:
                    return new ValueCamlNode(Type.Binary, binaryValue.ByteArrayToHex());
            }

            return new ValueCamlNode(Type.Text, value.ToString());
        }

        public static ValueCamlNode Parameter(string value)
        {
            return new ValueCamlNode(Type.Parameter, value);
        }

        public static ValueCamlNode System(string value)
        {
            return new ValueCamlNode(Type.System, value);
        }

        public override string ToString()
        {
            var node = new XElement(NodeType.ToString(), Value);
            return node.ToString(SaveOptions.DisableFormatting);
        }
    }

    public class MultipleValueCamlNode : CamlNode
    {
        public enum Type
        {
            Text,
            Integer,
            Parameter
        }

        public MultipleValueCamlNode(Type valueType, string[] values)
            : base(CamlNodeType.Values)
        {
            ValueType = valueType;
            Values = values;
        }

        public Type ValueType { get; }

        public string[] Values { get; }


        public static implicit operator MultipleValueCamlNode(string[] values)
        {
            return new MultipleValueCamlNode(Type.Text, values);
        }

        public static implicit operator MultipleValueCamlNode(int[] values)
        {
            return new MultipleValueCamlNode(Type.Integer,
                values.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray());
        }

        public static implicit operator MultipleValueCamlNode(long[] values)
        {
            return new MultipleValueCamlNode(Type.Integer,
                values.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray());
        }

        public static MultipleValueCamlNode Parameter(string[] values)
        {
            return new MultipleValueCamlNode(Type.Parameter, values);
        }

        public override string ToString()
        {
            var node = new XElement(NodeType.ToString(), Values.Csv());
            return node.ToString(SaveOptions.DisableFormatting);
        }
    }

    public enum CamlNodeType
    {
        Where,

        And,
        Or,

        Eq,
        Neq,
        Gt,
        Lt,
        Geq,
        Leq,

        Contains,
        NContains,
        BeginsWith,
        In,

        Not,
        IsNull,
        IsNotNull,
        Changed,
        NotChanged,

        FieldRef,
        Value,
        Values,

        Between
    }

    public class CamlNodeParser
    {
        public virtual CamlNode Parse(XElement xml)
        {
            if (xml == null)
                return null;

            var nodeType = (CamlNodeType)Enum.Parse(typeof(CamlNodeType), xml.Name.LocalName);

            switch (nodeType)
            {
                case CamlNodeType.Where:
                case CamlNodeType.Not:
                case CamlNodeType.IsNull:
                case CamlNodeType.IsNotNull:
                case CamlNodeType.Changed:
                case CamlNodeType.NotChanged:
                    return ParseUnary(nodeType, xml);
                case CamlNodeType.And:
                case CamlNodeType.Or:
                case CamlNodeType.BeginsWith:
                case CamlNodeType.Contains:
                case CamlNodeType.NContains:
                case CamlNodeType.In:
                case CamlNodeType.Between:
                case CamlNodeType.Eq:
                case CamlNodeType.Neq:
                case CamlNodeType.Lt:
                case CamlNodeType.Leq:
                case CamlNodeType.Gt:
                case CamlNodeType.Geq:
                    return ParseBinary(nodeType, xml);
                case CamlNodeType.FieldRef:
                    return ParseField(xml);
                case CamlNodeType.Value:
                    return ParseValue(xml);
                case CamlNodeType.Values:
                    return ParseMultipleValue(xml);
                default:
                    throw new Exception($"Unhandled node type: '{nodeType}'");
            }
        }

        protected virtual CamlNode ParseUnary(CamlNodeType nodeType, XElement xml)
        {
            return CamlNode.MakeUnary(nodeType,
                Parse(xml.Elements().FirstOrDefault()));
        }

        protected virtual CamlNode ParseBinary(CamlNodeType nodeType, XElement xml)
        {
            return CamlNode.MakeBinary(nodeType,
                Parse(xml.Elements().FirstOrDefault()),
                Parse(xml.Elements().Skip(1).FirstOrDefault()));
        }

        protected virtual CamlNode ParseField(XElement xml)
        {
            return CamlNode.MakeField((string)xml.Attribute("Name"));
        }

        protected virtual CamlNode ParseValue(XElement xml)
        {
            var sType = (string)xml.Attribute("Type");

            var valueType = string.IsNullOrEmpty(sType)
                ? ValueCamlNode.Type.Text
                : (ValueCamlNode.Type)Enum.Parse(typeof(ValueCamlNode.Type), sType);

            return CamlNode.MakeValue(valueType, xml.Value);
        }

        protected virtual CamlNode ParseMultipleValue(XElement xml)
        {
            var sType = (string)xml.Attribute("Type");

            var valueType = string.IsNullOrEmpty(sType)
                ? MultipleValueCamlNode.Type.Text
                : (MultipleValueCamlNode.Type)Enum.Parse(typeof(MultipleValueCamlNode.Type), sType);

            return CamlNode.MakeMultipleValue(valueType, xml.Value.SplitCsv().ToArray());
        }
    }

    public static class StringExtensions
    {
        private static readonly Regex ReIsValidPrincipalId = new Regex(@"^\w+\\\w+$", RegexOptions.Compiled);

        /// <summary>
        /// Checks whether string matches PrincipalId format ("DOMAIN\LOGIN")
        /// </summary>
        /// <param name="s">input string</param>
        /// <returns>Is string matches PrincipalId format</returns>
        public static bool IsValidPrincipalName(this string s)
        {
            return !string.IsNullOrEmpty(s) && ReIsValidPrincipalId.IsMatch(s);
        }

        public static string ToHTML(this string text)
        {
            var html = text.Replace("\r\n", "\n").Replace("\n", "<br>").Replace("\t", "&nbsp");
            return html;
        }

        public static bool ContainsAny(this IEnumerable<string> items, IEnumerable<string> values, IEqualityComparer<string> comparer = null)
        {
            if (items == null || values == null)
                return false;

            return items.Intersect(values, comparer ?? StringComparer.Ordinal).Any();
        }

        public static string Cut(this string value, int maxChars)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            return value.Substring(0, Math.Min(value.Length, maxChars));
        }

        public static string ToStringInvariant(this int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string EmptyIfNull(this string s)
        {
            return s ?? string.Empty;
        }

        public static string NullIfEmpty(this string s)
        {
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        public static string Csv(this IEnumerable<string> values)
        {
            if (values == null) return string.Empty;
            return string.Join(",", values.ToArray());
        }

        public static string Csv<T>(this IEnumerable<T> values)
        {
            if (values == null) return string.Empty;
            return string.Join(",", values.Select(x => x.ToString()).ToArray());
        }

        public static IEnumerable<string> SplitCsv(this string csv)
        {
            if (string.IsNullOrEmpty(csv)) return new string[] { };
            return csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static IEnumerable<string> SplitCsv(this string csv, params char[] delimiters)
        {
            if (string.IsNullOrEmpty(csv)) return new string[] { };
            return csv.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string JoinWith(this IEnumerable<string> source, string separator)
        {
            return string.Join(separator, source);
        }

        public static IEnumerable<string> WrapWith(this IEnumerable<string> source, string start, string end)
        {
            return source.Select(s => s.WrapWith(start, end));
        }

        public static IEnumerable<string> WrapWith(this IEnumerable<string> source, string wrapper)
        {
            return source.Select(s => s.WrapWith(wrapper));
        }

        public static string WrapWith(this string source, string start, string end)
        {
            return $"{start}{source}{end}";
        }

        public static string WrapWith(this string source, string wrapper)
        {
            return $"{wrapper}{source}{wrapper}";
        }

        public static byte[] GetBytes(this string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static string GetString(this byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        public static string ByteArrayToHex(this byte[] bytes)
        {
            var hex = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public static byte[] HexToByteArray(this string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static bool EqualsIgnoreCase(this string s1, string s2)
        {
            return String.Compare(s1, s2, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }

        public static bool ContainsIgnoreCase(this string source, string toCheck)
        {
            return source.Contains(toCheck, StringComparison.OrdinalIgnoreCase);
        }

        public static int ParseIntOrDefault(this string s)
        {
            int result;
            return int.TryParse(s, out result) ? result : 0;
        }

        public static string TrimLineEndings(this string str)
        {
            if (String.IsNullOrEmpty(str))
                return String.Empty;

            return str.Trim('\r', '\n');
        }

        public static string NormalizeLineEndings(this string str)
        {
            return str.Replace("\\n", Environment.NewLine);
        }

        /// <summary>
        /// Убирает из строки управляющие Unicode-символы (0x00 - 0x1F) за исключением 0x0A 0x0D (перевод строки).
        /// </summary>
        /// <param name="str">исходная строка</param>
        /// <returns>нормализованная строка</returns>
        public static string NormalizeUnicodeControlChars(this string str)
        {
            if (String.IsNullOrEmpty(str) || String.IsNullOrEmpty(str.Trim()))
                return String.Empty;

            string re = @"[\x00-\x09]|[\x0B-\x0C]|[\x0E-\x1F]";
            return Regex.Replace(str, re, "");
        }

        /// <summary>
        /// Убирает из строки управляющие Unicode-символы (0x00 - 0x1F) с учетом 0x0A 0x0D (перевод строки).
        /// </summary>
        /// <param name="str">исходная строка</param>
        /// <returns>нормализованная строка</returns>
        public static string NormalizeUnicodeControlCharsAndNewLine(this string str)
        {
            if (String.IsNullOrEmpty(str) || String.IsNullOrEmpty(str.Trim()))
                return String.Empty;

            string re = @"[\x00-\x1F]";
            return Regex.Replace(str, re, "");
        }

        /// <summary>
        /// Приведение переводов строк в HTML формат (для рассылки HTML-писем)
        /// </summary>
        /// <param name="str">исходная строка</param>
        /// <returns>нормализованная строка</returns>
        public static string ConvertNewLineToHtml(this string str)
        {
            if (String.IsNullOrEmpty(str) || String.IsNullOrEmpty(str.Trim()))
                return String.Empty;

            return str.Replace(Environment.NewLine, "<br />");
        }

        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        public static string TruncateWithEllipsis(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 3) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
        }

        public static string EscapeJson(this string value)
        {
            string re = @"[\x00-\x1F]";
            string s = Regex.Replace(value, re, "");
            return s.Replace("\\", "\\\\").Replace("/", "\\/").Replace("\"", "\\\"");
        }
        public static string EncodeAngleBrackets(this string value)
        {
            return value.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        public static string DomainNameFromLogin(this string loginName)
        {
            if (string.IsNullOrEmpty(loginName))
                throw new ArgumentNullException("loginName");

            return loginName.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Last()
                .Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries)
                .First();
        }

        #region Email

        private static readonly Regex ReEmailGroups = new Regex(@"(@)(.+)$", RegexOptions.Compiled);

        // Обращение 557582: упростить контроль адресов эл. почты.
        // расширенная проверка перед @ - ^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))
        // расширенная проверка после @ - (?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,17}))$
        private static readonly Regex ReEmailFormat = new Regex(
                   @"^(([\w](\.|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@)" +
                   @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[0-9a-z]{2,17}))$",
                   RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool IsValidEmail(this string str)
        {
            bool invalid = false;
            if (string.IsNullOrEmpty(str))
                return false;

            // Use IdnMapping class to convert Unicode domain names.
            str = ReEmailGroups.Replace(str, match =>
            {
                // IdnMapping class with default property values.
                var idn = new IdnMapping();

                string domainName = match.Groups[2].Value;
                try
                {
                    domainName = idn.GetAscii(domainName);
                }
                catch (ArgumentException)
                {
                    invalid = true;
                }
                return match.Groups[1].Value + domainName;
            });

            // Return true if str is in valid e-mail format.
            return !invalid && ReEmailFormat.IsMatch(str);
        }

        #endregion

        /// <summary>
        /// Метод разбивает строку на части по указанным символам не более указанной длины, в конец каждой части добавляется строка-разделитель
        /// </summary>
        /// <param name="text">Обрабатываемая строка</param>
        /// <param name="splitOnCharacters">Символы, по которым разбивается строка</param>
        /// <param name="partLength">Максимальная длина частей</param>
        /// <param name="delimiter">Строка, добавляемая к каждой части</param>
        /// <returns>Отформатированная строка</returns>
        public static string SplitToParts(this string text, char[] splitOnCharacters, int partLength, string delimiter)
        {
            var result = new StringBuilder();
            var index = 0;

            while (text.Length > index)
            {
                // Добавляем после каждой части строку-разделитель
                if (index != 0)
                    result.Append(delimiter);

                // ищем позицию окончанию следующей подстроки или берем оставшуюся часть строки
                var splitAt = index + partLength <= text.Length ?
                                text.Substring(index, partLength).LastIndexOfAny(splitOnCharacters) :
                                text.Length - index;

                // если что берем максимальную длину подстроки
                splitAt = (splitAt == -1) ? partLength : splitAt;

                result.Append(text.Substring(index, splitAt).Trim());
                index += splitAt;
            }

            return result.ToString();
        }

        /// <summary>
        /// Привести слово в соответствие нотации CamelCase
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ToLowerCamelCase(this string str)
        {
            if (!string.IsNullOrEmpty(str) && str.Length > 1)
            {
                return char.ToLowerInvariant(str[0]) + str.Substring(1);
            }

            return str;
        }

        /// <summary>
        /// Привести слово в соответствие нотации CamelCase
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ToUpperCamelCase(this string str)
        {
            if (!string.IsNullOrEmpty(str) && str.Length > 1)
            {
                return char.ToUpperInvariant(str[0]) + str.Substring(1);
            }

            return str;
        }
    }

    public static class CamlNodeExtensions
    {
        public static object Value(this ValueCamlNode valueNode)
        {
            switch (valueNode.ValueType)
            {
                case ValueCamlNode.Type.DateTime:
                    return valueNode.DateValue();
                case ValueCamlNode.Type.Integer:
                    return valueNode.IntValue();
                case ValueCamlNode.Type.Binary:
                    return valueNode.BinaryValue();
                default:
                    return valueNode.Value;
            }
        }

        public static IEnumerable<object> Values(this MultipleValueCamlNode valueNode)
        {
            switch (valueNode.ValueType)
            {
                case MultipleValueCamlNode.Type.Integer:
                    return valueNode.IntValues().Cast<object>();
                default:
                    return valueNode.Values;
            }
        }

        public static DateTime DateValue(this ValueCamlNode valueNode)
        {
            if (valueNode.ValueType != ValueCamlNode.Type.DateTime)
                throw new InvalidOperationException();

            if (!valueNode.Value.TryParseDate(out var dateTime))
                throw new Exception($"Invalid DateTime value: '{valueNode.Value}'");

            return dateTime;
        }

        public static DateTime?[] DateValues(this ValueCamlNode valueNode)
        {
            if (valueNode.ValueType != ValueCamlNode.Type.DateTime)
                throw new InvalidOperationException();

            var values = (valueNode.Value ?? string.Empty).Split(new[] { '&' }, StringSplitOptions.None);

            var result = new List<DateTime?>();
            foreach (var v in values)
            {
                result.Add(v.TryParseDate(out var dateTime) ? dateTime : (DateTime?)null);
            }

            return result.ToArray();
        }

        public static int IntValue(this ValueCamlNode valueNode)
        {
            if (valueNode.ValueType != ValueCamlNode.Type.Integer)
                throw new InvalidOperationException();

            return int.TryParse(valueNode.Value, out var integer) ? integer : 0;
        }

        public static IEnumerable<int> IntValues(this MultipleValueCamlNode valueNode)
        {
            if (valueNode.ValueType != MultipleValueCamlNode.Type.Integer)
                throw new InvalidOperationException();

            return valueNode.Values.Select(value => int.TryParse(value, out int integer) ? integer : 0);
        }

        public static byte[] BinaryValue(this ValueCamlNode valueNode)
        {
            if (valueNode.ValueType != ValueCamlNode.Type.Binary)
                throw new InvalidOperationException();

            return valueNode.Value.HexToByteArray();
        }
    }

    public abstract class CamlNodeVisitor
    {
        protected virtual CamlNode Visit(CamlNode node)
        {
            if (node == null)
                return null;

            switch (node.NodeType)
            {
                case CamlNodeType.Where:
                case CamlNodeType.Not:
                case CamlNodeType.IsNull:
                case CamlNodeType.IsNotNull:
                case CamlNodeType.Changed:
                    return VisitUnary((UnaryCamlNode)node);
                case CamlNodeType.And:
                case CamlNodeType.Or:
                case CamlNodeType.BeginsWith:
                case CamlNodeType.Contains:
                case CamlNodeType.NContains:
                case CamlNodeType.In:
                case CamlNodeType.Between:
                case CamlNodeType.Eq:
                case CamlNodeType.Neq:
                case CamlNodeType.Lt:
                case CamlNodeType.Leq:
                case CamlNodeType.Gt:
                case CamlNodeType.Geq:
                    return VisitBinary((BinaryCamlNode)node);
                case CamlNodeType.FieldRef:
                    return VisitField((FieldRefCamlNode)node);
                case CamlNodeType.Value:
                    return VisitValue((ValueCamlNode)node);
                case CamlNodeType.Values:
                    return VisitMultipleValue((MultipleValueCamlNode)node);
                default:
                    throw new Exception($"Unhandled node type: '{node.NodeType}'");
            }
        }

        protected virtual CamlNode VisitUnary(UnaryCamlNode u)
        {
            var node = Visit(u.Node);
            return node != u.Node ? CamlNode.MakeUnary(u.NodeType, node) : u;
        }

        protected virtual CamlNode VisitBinary(BinaryCamlNode b)
        {
            var left = Visit(b.Left);
            var right = Visit(b.Right);
            if (left != b.Left || right != b.Right)
            {
                return CamlNode.MakeBinary(b.NodeType, left, right);
            }
            return b;
        }

        protected virtual CamlNode VisitField(FieldRefCamlNode f)
        {
            return f;
        }

        protected virtual CamlNode VisitValue(ValueCamlNode v)
        {
            return v;
        }

        protected virtual CamlNode VisitMultipleValue(MultipleValueCamlNode v)
        {
            return v;
        }
    }

    /// <summary>
    /// Заменяет узлы типа Значение в фильтре на параметры
    /// (параметризует запрос)
    /// </summary>
    internal sealed class InjectParametersCamlNodeVisitor : CamlNodeVisitor
    {
        private readonly IList<KeyValuePair<string, object>> _parameters;

        public InjectParametersCamlNodeVisitor(IList<KeyValuePair<string, object>> parameters)
        {
            _parameters = parameters;
        }

        public CamlNode ReplaceValuesWithParameters(CamlNode node)
        {
            return Visit(node);
        }

        protected override CamlNode VisitValue(ValueCamlNode v)
        {
            if (v.ValueType == ValueCamlNode.Type.Parameter || v.ValueType == ValueCamlNode.Type.System)
                return v;

            string parameterName = "p" + _parameters.Count;
            _parameters.Add(new KeyValuePair<string, object>(parameterName, v.Value().ToSqlValue()));

            return ValueCamlNode.Parameter(parameterName);
        }

        protected override CamlNode VisitMultipleValue(MultipleValueCamlNode v)
        {
            if (v.ValueType == MultipleValueCamlNode.Type.Parameter)
                return v;

            var parameterNames = new List<string>();

            foreach (var value in v.Values())
            {
                string parameterName = "p" + _parameters.Count;
                parameterNames.Add(parameterName);

                _parameters.Add(new KeyValuePair<string, object>(parameterName, value.ToSqlValue()));
            }

            return MultipleValueCamlNode.Parameter(parameterNames.ToArray());
        }
    }
}
