using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Csud.Base
{
    
    public static class ConvertExtensions
    {
        public static TTarget Convert<TTarget>(this object obj)
        {
            return (TTarget) Convert(obj, typeof(TTarget));
        }

        public static object Convert(this object obj, Type targetType)
        {
            if (targetType.IsEnum)
            {
                if (obj == null || Equals(obj, string.Empty))
                    return Activator.CreateInstance(targetType);
                return System.Convert.ChangeType(obj, typeof(int));
            }

            // Обработка Nullable типов
            Type underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                if (obj == null || Equals(obj, string.Empty))
                    return null;
                return System.Convert.ChangeType(obj, underlyingType);
            }

            try
            {
                return System.Convert.ChangeType(obj, targetType);

            }
            catch (Exception e)
            {
                return obj.ToString();
            }
          
        }

        public static T ParseEnum<T>(this string enumString) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException(typeof(T).Name);

            return (T) Enum.Parse(typeof(T), enumString);
        }

        public static bool TryParseGuid(this string item, out Guid value)
        {
            value = Guid.Empty;
            try
            {
                value = new Guid(item);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }

    public static class ReflectionExtensions
    {
        public static bool IsNumericType(this object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsIntegerType(this object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return true;
                default:
                    return false;
            }
        }

        public static T GetPropertyValue<T>(this object obj, string propertyName)
        {
            var propertyInfo = obj.GetType().GetProperty(propertyName);
            return (T)propertyInfo.GetValue(obj, null);
        }

        public static void SetPropertyValues<T>(this T obj, IEnumerable<KeyValuePair<string, object>> propertyValues)
        {
            SetPropertyValues(obj, propertyValues, null);
        }

        public static void SetPropertyValues<T>(this T obj, IEnumerable<KeyValuePair<string, object>> propertyValues, Action<T, string, object> propertyNotFoundHandler)
        {
            Type type = typeof(T);

            foreach (var item in propertyValues)
            {
                var propertyInfo = type.GetProperty(item.Key);
                if (propertyInfo != null)
                {
                    propertyInfo.SetValue(obj, item.Value, null);
                    continue;
                }
                if (propertyNotFoundHandler != null)
                    propertyNotFoundHandler(obj, item.Key, item.Value);
            }
        }

        public static IList<PropertyInfo> GetIndexProperties(this Type type)
        {
            IList<PropertyInfo> results = new List<PropertyInfo>();

            var props = type.GetProperties(BindingFlags.Default | BindingFlags.Public | BindingFlags.Instance);

            if (props != null)
            {
                foreach (var prop in props)
                {
                    var indexParameters = prop.GetIndexParameters();
                    if (indexParameters == null || indexParameters.Length == 0)
                    {
                        continue;
                    }
                    results.Add(prop);
                }
            }

            return results;
        }
    }

    public static class DateTimeExtensions
    {
        public static string Serialize(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        public static bool TryParseDate(this string s, out DateTime dateTime)
        {
            return DateTime.TryParse(s, out dateTime);
        }

        private static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
        {
            return timeSpan == TimeSpan.Zero
                ? dateTime
                : dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }

        public static DateTime RoundToSeconds(this DateTime dateTime)
        {
            return Truncate(dateTime, TimeSpan.FromSeconds(1));
        }

        public static DateTime RoundToDays(this DateTime dateTime)
        {
            return Truncate(dateTime, TimeSpan.FromDays(1));
        }

        public static bool TryParseTime(this string value, out TimeSpan timeSpan)
        {
            timeSpan = TimeSpan.Zero;

            Int32 h, m, s;
            var st = value.Split(':');
            if (st == null)
                return false;

            if (st.Length == 2)
            {
                if (!Int32.TryParse(st[0], out h) || !Int32.TryParse(st[1], out m))
                    return false;

                timeSpan = new TimeSpan(h, m, 0);
            }
            else if (st.Length >= 3)
            {
                if (!Int32.TryParse(st[0], out h) || !Int32.TryParse(st[1], out m) || !Int32.TryParse(st[2], out s))
                    return false;

                timeSpan = new TimeSpan(h, m, s);
            }

            return timeSpan != TimeSpan.Zero ? true : false;
        }
    }

    internal static class SqlUtility
    {
        public static string ToTitleCase(this string text)
        {
            TextInfo myTI = new CultureInfo("en-US", false).TextInfo;
            return myTI.ToTitleCase(text).Replace("_", "");
        }
        public static string ToSnakeCase(this string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }
            if (text.Length < 2)
            {
                return text;
            }
            var sb = new StringBuilder();
            sb.Append(char.ToLowerInvariant(text[0]));
            for (int i = 1; i < text.Length; ++i)
            {
                char c = text[i];
                if (char.IsUpper(c))
                {
                    sb.Append('_');
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static string ToSqlString(this DateTime dateTime)
        {
            // Максимальная точность, которую поддерживает SQL Server для типа datetime .00333 секунды
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss.ff");
        }

        public static object ToSqlValue(this object value)
        {
            if (!(value is DateTime))
                return value;

            var dateValue = (DateTime)value;
            // Максимальная точность, которую поддерживает SQL Server для типа datetime .00333 секунды
            return new DateTime(dateValue.Year, dateValue.Month, dateValue.Day, dateValue.Hour,
                dateValue.Minute, dateValue.Second, dateValue.Millisecond / 10 * 10);
        }
    }
}
