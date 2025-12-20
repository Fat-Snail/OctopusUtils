using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Util.Helpers;

/// <summary>
/// 类型转换
/// </summary>
public static class Convert
{
    /// <summary>
    /// 安全转换为字符串，去除两端空格，当值为null时返回""
    /// </summary>
    /// <param name="input">输入值</param>
    public static String SafeString(this Object input)
    {
        return input?.ToString()?.Trim() ?? String.Empty;
    }

    #region ToInt(转换为32位整型)

    /// <summary>
    /// 转换为32位整型
    /// </summary>
    /// <param name="input">输入值</param>
    public static Int32 ToInt(Object input)
    {
        return ToIntOrNull(input) ?? 0;
    }

    #endregion

    #region ToIntOrNull(转换为32位可空整型)

    /// <summary>
    /// 转换为32位可空整型
    /// </summary>
    /// <param name="input">输入值</param>
    public static Int32? ToIntOrNull(Object input)
    {
        var success = Int32.TryParse(input.SafeString(), out var result);
        if (success)
            return result;
        try
        {
            var temp = ToDoubleOrNull(input, 0);
            if (temp == null)
                return null;
            return System.Convert.ToInt32(temp);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region ToLong(转换为64位整型)

    /// <summary>
    /// 转换为64位整型
    /// </summary>
    /// <param name="input">输入值</param>
    public static Int64 ToLong(Object input)
    {
        return ToLongOrNull(input) ?? 0;
    }

    #endregion

    #region ToLongOrNull(转换为64位可空整型)

    /// <summary>
    /// 转换为64位可空整型
    /// </summary>
    /// <param name="input">输入值</param>
    public static Int64? ToLongOrNull(Object input)
    {
        var success = Int64.TryParse(input.SafeString(), out var result);
        if (success)
            return result;
        try
        {
            var temp = ToDecimalOrNull(input, 0);
            if (temp == null)
                return null;
            return System.Convert.ToInt64(temp);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region ToFloat(转换为32位浮点型)

    /// <summary>
    /// 转换为32位浮点型,并按指定小数位舍入
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="digits">小数位数</param>
    public static Single ToFloat(Object input, Int32? digits = null)
    {
        return ToFloatOrNull(input, digits) ?? 0;
    }

    #endregion

    #region ToFloatOrNull(转换为32位可空浮点型)

    /// <summary>
    /// 转换为32位可空浮点型,并按指定小数位舍入
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="digits">小数位数</param>
    public static Single? ToFloatOrNull(Object input, Int32? digits = null)
    {
        var success = Single.TryParse(input.SafeString(), out var result);
        if (!success)
            return null;
        if (digits == null)
            return result;
        return (Single)Math.Round(result, digits.Value);
    }

    #endregion

    #region ToDouble(转换为64位浮点型)

    /// <summary>
    /// 转换为64位浮点型,并按指定小数位舍入
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="digits">小数位数</param>
    public static Double ToDouble(Object input, Int32? digits = null)
    {
        return ToDoubleOrNull(input, digits) ?? 0;
    }

    #endregion

    #region ToDoubleOrNull(转换为64位可空浮点型)

    /// <summary>
    /// 转换为64位可空浮点型,并按指定小数位舍入
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="digits">小数位数</param>
    public static Double? ToDoubleOrNull(Object input, Int32? digits = null)
    {
        var success = Double.TryParse(input.SafeString(), out var result);
        if (!success)
            return null;
        if (digits == null)
            return result;
        return Math.Round(result, digits.Value);
    }

    #endregion

    #region ToDecimal(转换为128位浮点型)

    /// <summary>
    /// 转换为128位浮点型,并按指定小数位舍入
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="digits">小数位数</param>
    public static Decimal ToDecimal(Object input, Int32? digits = null)
    {
        return ToDecimalOrNull(input, digits) ?? 0;
    }

    #endregion

    #region ToDecimalOrNull(转换为128位可空浮点型)

    /// <summary>
    /// 转换为128位可空浮点型,并按指定小数位舍入
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="digits">小数位数</param>
    public static Decimal? ToDecimalOrNull(Object input, Int32? digits = null)
    {
        var success = Decimal.TryParse(input.SafeString(), out var result);
        if (!success)
            return null;
        if (digits == null)
            return result;
        return Math.Round(result, digits.Value);
    }

    #endregion

    #region ToBool(转换为布尔值)

    /// <summary>
    /// 转换为布尔值
    /// </summary>
    /// <param name="input">输入值</param>
    public static Boolean ToBool(Object input)
    {
        return ToBoolOrNull(input) ?? false;
    }

    #endregion

    #region ToBoolOrNull(转换为可空布尔值)

    /// <summary>
    /// 转换为可空布尔值
    /// </summary>
    /// <param name="input">输入值</param>
    public static Boolean? ToBoolOrNull(Object input)
    {
        var value = input.SafeString();
        switch (value)
        {
            case "1":
                return true;
            case "0":
                return false;
        }

        return Boolean.TryParse(value, out var result) ? result : null;
    }

    #endregion

    #region ToDateTime(转换为日期)

    /// <summary>
    /// 转换为日期
    /// </summary>
    /// <param name="input">输入值</param>
    public static DateTime ToDateTime(Object input)
    {
        return ToDateTimeOrNull(input) ?? DateTime.MinValue;
    }

    #endregion

    #region ToDateTimeOrNull(转换为可空日期)

    /// <summary>
    /// 转换为可空日期
    /// </summary>
    /// <param name="input">输入值</param>
    public static DateTime? ToDateTimeOrNull(Object input)
    {
        var success = DateTime.TryParse(input.SafeString(), out var result);
        if (success == false)
            return null;
        return result;
    }

    #endregion

    #region ToGuid(转换为Guid)

    /// <summary>
    /// 转换为Guid
    /// </summary>
    /// <param name="input">输入值</param>
    public static Guid ToGuid(Object input)
    {
        return ToGuidOrNull(input) ?? Guid.Empty;
    }

    #endregion

    #region ToGuidOrNull(转换为可空Guid)

    /// <summary>
    /// 转换为可空Guid
    /// </summary>
    /// <param name="input">输入值</param>
    public static Guid? ToGuidOrNull(Object input)
    {
        if (input == null)
            return null;
        if (input.GetType() == typeof(Byte[]))
            return new Guid((Byte[])input);
        return Guid.TryParse(input.SafeString(), out var result) ? result : null;
    }

    #endregion

    #region ToGuidList(转换为Guid集合)

    /// <summary>
    /// 转换为Guid集合
    /// </summary>
    /// <param name="input">以逗号分隔的Guid集合字符串，范例:83B0233C-A24F-49FD-8083-1337209EBC9A,EAB523C6-2FE7-47BE-89D5-C6D440C3033A</param>
    public static List<Guid> ToGuidList(String input)
    {
        return ToList<Guid>(input);
    }

    #endregion

    #region ToBytes(转换为字节数组)

    /// <summary>
    /// 转换为字节数组
    /// </summary>
    /// <param name="input">输入值</param>        
    public static Byte[] ToBytes(String input)
    {
        return ToBytes(input, Encoding.UTF8);
    }

    /// <summary>
    /// 转换为字节数组
    /// </summary>
    /// <param name="input">输入值</param>
    /// <param name="encoding">字符编码</param>
    public static Byte[] ToBytes(String input, Encoding encoding)
    {
        return String.IsNullOrWhiteSpace(input) ? new Byte[] { } : encoding.GetBytes(input);
    }

    #endregion

    // #region ToBase64(转换为base64字符串)
    //
    // /// <summary>
    // /// 转换为base64字符串
    // /// </summary>
    // /// <param name="input">输入值</param>        
    // public static string ToBase64(string input)
    // {
    //     return input.IsEmpty() ? null : System.Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
    // }
    //
    // #endregion

    #region ToList(泛型集合转换)

    /// <summary>
    /// 泛型集合转换
    /// </summary>
    /// <typeparam name="T">目标元素类型</typeparam>
    /// <param name="input">以逗号分隔的元素集合字符串，范例:83B0233C-A24F-49FD-8083-1337209EBC9A,EAB523C6-2FE7-47BE-89D5-C6D440C3033A</param>
    public static List<T> ToList<T>(String input)
    {
        var result = new List<T>();
        if (String.IsNullOrWhiteSpace(input))
            return result;
        var array = input.Split(',');
        result.AddRange(from each in array where !String.IsNullOrWhiteSpace(each) select To<T>(each));
        return result;
    }

    #endregion

    #region To(通用泛型转换)

    /// <summary>
    /// 通用泛型转换
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="input">输入值</param>
    public static T To<T>(Object input)
    {
        if (input == null)
            return default;
        if (input is String && String.IsNullOrWhiteSpace(input.ToString()))
            return default;
        var type = Common.GetType<T>();
        var typeName = type.Name.ToUpperInvariant();
        try
        {
            if (typeName == "STRING" || typeName == "GUID")
                return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(input.ToString());
            // if (type.IsEnum)
            //     return Enum.Parse<T>(input);
            if (input is IConvertible)
                return (T)System.Convert.ChangeType(input, type, CultureInfo.InvariantCulture);
            // if (input is JsonElement element)
            // {
            //     var value = element.GetRawText();
            //     var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            //     return Json.ToObject<T>(value, options);
            // }

            return (T)input;
        }
        catch
        {
            return default;
        }
    }

    #endregion

    #region ToDictionary(对象转换为属性名值对)

    /// <summary>
    /// 对象转换为属性名值对
    /// </summary>
    /// <param name="data">对象</param>
    public static IDictionary<String, Object> ToDictionary(Object data)
    {
        return ToDictionary(data, false);
    }

    /// <summary>
    /// 对象转换为属性名值对
    /// </summary>
    /// <param name="data">对象</param>
    /// <param name="useDisplayName">是否使用显示名称,可使用[Description] 或 [DisplayName]特性设置</param>
    public static IDictionary<String, Object> ToDictionary(Object data, Boolean useDisplayName)
    {
        var result = new Dictionary<String, Object>();
        if (data == null)
            return result;
        if (data is IEnumerable<KeyValuePair<String, Object>> dic)
            return new Dictionary<String, Object>(dic);
        foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(data))
        {
            var value = property.GetValue(data);
            result.Add(GetPropertyDescriptorName(property, useDisplayName), value);
        }

        return result;
    }

    /// <summary>
    /// 获取属性名
    /// </summary>
    private static String GetPropertyDescriptorName(PropertyDescriptor property, Boolean useDisplayName)
    {
        if (useDisplayName == false)
            return property.Name;
        if (String.IsNullOrEmpty(property.Description) == false)
            return property.Description;
        if (String.IsNullOrEmpty(property.DisplayName) == false)
            return property.DisplayName;
        return property.Name;
    }

    #endregion
}