using System.ComponentModel;
using System.Globalization;
using Lucene.Net.Documents;

namespace Octopus.SearchCore.Ext;

public static class DocumentExtension
{
    /// <summary>
    /// 获取文档的值
    /// </summary>
    /// <param name="doc">Lucene文档</param>
    /// <param name="key">键</param>
    /// <param name="t">类型</param>
    /// <returns></returns>
    internal static Object Get(this Document doc, String key, Type t)
    {
        var value = doc.Get(key);
        return t switch
        {
            _ when t.IsAssignableFrom(typeof(String)) => value,
            _ when t.IsValueType => ConvertTo(value, t),
            _ => System.Text.Json.JsonSerializer.Deserialize(value, t) //JsonConvert.DeserializeObject(value, t)
        };
    }

    /// <summary>
    /// 类型直转
    /// </summary>
    /// <param name="value"></param>
    /// <param name="type">目标类型</param>
    /// <returns></returns>
    private static Object ConvertTo(String value, Type type)
    {
        if (value == null)
        {
            return default;
        }

        if (value.GetType() == type)
        {
            return value;
        }

        if (type.IsEnum)
        {
            return Enum.Parse(type, value.ToString(CultureInfo.InvariantCulture));
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            return underlyingType!.IsEnum ? Enum.Parse(underlyingType, value.ToString(CultureInfo.CurrentCulture)) : Convert.ChangeType(value, underlyingType);
        }

        var converter = TypeDescriptor.GetConverter(value);
        if (converter != null)
        {
            if (converter.CanConvertTo(type))
            {
                return converter.ConvertTo(value, type);
            }
        }

        converter = TypeDescriptor.GetConverter(type);
        if (converter != null)
        {
            if (converter.CanConvertFrom(value.GetType()))
            {
                return converter.ConvertFrom(value);
            }
        }

        return Convert.ChangeType(value, type);
    }
}