namespace Util.Helpers;

/// <summary>
/// 公共操作
/// </summary>
public static class Common
{
    /// <summary>
    /// 获取当前应用程序基路径
    /// </summary>
    public static String ApplicationBaseDirectory => AppContext.BaseDirectory;

    /// <summary>
    /// 换行符
    /// </summary>
    public static String Line => System.Environment.NewLine;
    // /// <summary>
    // /// 是否Linux操作系统
    // /// </summary>
    // public static bool IsLinux => RuntimeInformation.IsOSPlatform( OSPlatform.Linux );
    // /// <summary>
    // /// 是否Windows操作系统
    // /// </summary>
    // public static bool IsWindows => RuntimeInformation.IsOSPlatform( OSPlatform.Windows );

    /// <summary>
    /// 获取类型
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    public static Type GetType<T>()
    {
        return GetType(typeof(T));
    }

    /// <summary>
    /// 获取类型
    /// </summary>
    /// <param name="type">类型</param>
    public static Type GetType(Type type)
    {
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    /// <summary>
    /// 获取物理路径
    /// </summary>
    /// <param name="relativePath">相对路径,范例:"test/a.txt" 或 "/test/a.txt"</param>
    /// <param name="basePath">基路径,默认为AppContext.BaseDirectory</param>
    public static String GetPhysicalPath(String relativePath, String basePath = null)
    {
        if ( relativePath.StartsWith("~") )
            relativePath = relativePath.TrimStart('~');
        if ( relativePath.StartsWith("/") )
            relativePath = relativePath.TrimStart('/');
        if ( relativePath.StartsWith("\\") )
            relativePath = relativePath.TrimStart('\\');
        basePath ??= ApplicationBaseDirectory;
        return Path.Combine(basePath, relativePath);
    }

    // /// <summary>
    // /// 连接路径
    // /// </summary>
    // /// <param name="paths">路径列表</param>
    // public static string JoinPath( params string[] paths ) {
    //     return Url.JoinPath( paths );
    // }

    /// <summary>
    /// 获取当前目录路径
    /// </summary>
    public static String GetCurrentDirectory()
    {
        return Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// 获取当前目录的上级路径
    /// </summary>
    /// <param name="depth">向上钻取的深度</param>
    public static String GetParentDirectory(Int32 depth = 1)
    {
        var path = Directory.GetCurrentDirectory();
        for ( Int32 i = 0; i < depth; i++ )
        {
            var parent = Directory.GetParent(path);
            if ( parent is { Exists: true } )
                path = parent.FullName;
        }

        return path;
    }

    /// <summary>
    /// 获取环境名称
    /// </summary>
    public static String GetEnvironmentName()
    {
        var environment = GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if ( environment.IsEmpty() == false )
            return environment;
        return GetEnvironmentVariable("DOTNET_ENVIRONMENT");
    }

    /// <summary>
    /// 获取环境变量
    /// </summary>
    /// <param name="name">环境变量名</param>
    public static String GetEnvironmentVariable(String name)
    {
        return System.Environment.GetEnvironmentVariable(name);
    }

    /// <summary>
    /// 获取环境变量
    /// </summary>
    /// <param name="name">环境变量名</param>
    public static T GetEnvironmentVariable<T>(String name)
    {
        return Convert.To<T>(GetEnvironmentVariable(name));
    }

    /// <summary>
    /// 是否为空
    /// </summary>
    /// <param name="value">值</param>
    public static Boolean IsEmpty([NotNullWhen(false)] this String? value)
    {
        return String.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// 是否为空
    /// </summary>
    /// <param name="value">值</param>
    public static Boolean IsEmpty(this Guid value)
    {
        return value == Guid.Empty;
    }

    /// <summary>
    /// 是否为空
    /// </summary>
    /// <param name="value">值</param>
    public static Boolean IsEmpty([NotNullWhen(false)] this Guid? value)
    {
        if ( value == null )
            return true;
        return value == Guid.Empty;
    }
}
