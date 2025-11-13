using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace Orcamentaria.Lib.Infrastructure.Helpers
{
    public static class ConvertHelper
    {
        public static object? ConvertTo(object? value, Type targetType)
        {
            if (value is null) return null;

            var t = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (value is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Null) return null;

                if (je.ValueKind == JsonValueKind.Array)
                    throw new InvalidCastException("Json array recebido onde era esperado valor escalar.");

                value = je.ValueKind switch
                {
                    JsonValueKind.String => je.GetString()!,
                    JsonValueKind.Number => je.TryGetInt64(out var l) ? (object)l : je.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => je.ToString()!
                };
            }

            if (t.IsEnum)
            {
                if (value is string s)
                {
                    if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                        return Enum.ToObject(t, ChangeNumericType(l, Enum.GetUnderlyingType(t)));
                    return Enum.Parse(t, s, ignoreCase: true);
                }

                if (IsNumeric(value.GetType()))
                    return Enum.ToObject(t, ChangeNumericType(value, Enum.GetUnderlyingType(t)));

                return Enum.Parse(t, value.ToString()!, ignoreCase: true);
            }

            if (t == typeof(Guid))
                return value is Guid g ? g : Guid.Parse(value.ToString()!, CultureInfo.InvariantCulture);

            if (t == typeof(DateTime))
                return value is DateTime dt ? dt : DateTime.Parse(value.ToString()!, CultureInfo.InvariantCulture);

            if (t == typeof(bool))
            {
                if (value is bool b) return b;
                var s = value.ToString()!.Trim();
                if (s == "1") return true;
                if (s == "0") return false;
                return bool.Parse(s);
            }

            if (value is string str)
            {
                var conv = TypeDescriptor.GetConverter(t);
                if (conv is not null && conv.CanConvertFrom(typeof(string)))
                    return conv.ConvertFromInvariantString(str);

                return System.Convert.ChangeType(str, t, CultureInfo.InvariantCulture);
            }

            if (value is IConvertible)
                return System.Convert.ChangeType(value, t, CultureInfo.InvariantCulture);

            if (t.IsInstanceOfType(value)) return value;

            throw new InvalidCastException($"Não foi possível converter de {value.GetType().Name} para {t.Name}.");
        }

        public static object[] ToArrayFor(Type targetType, object? value)
        {
            if (value is null) return Array.Empty<object>();
            var t = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (value is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Array)
                {
                    var list = new List<object>();
                    foreach (var el in je.EnumerateArray())
                    {
                        object scalar = el.ValueKind switch
                        {
                            JsonValueKind.String => el.GetString()!,
                            JsonValueKind.Number => el.TryGetInt64(out var l) ? (object)l : el.GetDouble(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            _ => el.ToString()!
                        };
                        list.Add(ConvertHelper.ConvertTo(scalar, t)!);
                    }
                    return list.ToArray();
                }

                var single = je.ValueKind switch
                {
                    JsonValueKind.String => je.GetString()!,
                    JsonValueKind.Number => je.TryGetInt64(out var l) ? (object)l : je.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => je.ToString()!
                };
                return new[] { ConvertHelper.ConvertTo(single, t)! };
            }

            if (value is string s)
            {
                var parts = s.Trim('[', ']', ' ')
                             .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return parts.Select(p => ConvertHelper.ConvertTo(p, t)!).ToArray();
            }

            if (value is IEnumerable en && value is not string)
            {
                var list = new List<object>();
                foreach (var item in en)
                    list.Add(ConvertHelper.ConvertTo(item, t)!);
                return list.ToArray();
            }

            return new[] { ConvertHelper.ConvertTo(value, t)! };
        }

        private static bool IsNumeric(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) ||
                   type == typeof(decimal);
        }

        private static object ChangeNumericType(object value, Type targetNumeric)
            => System.Convert.ChangeType(value, targetNumeric, CultureInfo.InvariantCulture)!;
    }
}
