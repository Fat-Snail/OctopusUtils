internal class ResourceHelper
{
    /// <summary>
    ///  Current Assembly
    /// </summary>
    private static Assembly asm = null;

    /// <summary>
    /// Current Assembly
    /// </summary>
    private static Assembly Asm
    {
        get
        {
            if ( asm == null )
                asm = Assembly.GetExecutingAssembly();
            return asm;
        }
    }

    /// <summary>
    /// resource (mainly file in file system or file in compressed package) as BufferedInputStream
    /// </summary>
    /// <param name="resourceName">resourceName</param>
    /// <returns></returns>
    public static Stream? GetResourceInputStream(String resourceName)
    {
        // 优先精确匹配（AssemblyName 前缀）
        var exact = Asm.GetManifestResourceStream($"{Asm.GetName().Name}.{resourceName}");
        if (exact != null) return exact;

        // 后缀匹配：兼容 AssemblyName ≠ RootNamespace 的场景
        var suffix = resourceName.Replace('/', '.').Replace('\\', '.');
        var match = Asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        return match != null ? Asm.GetManifestResourceStream(match) : null;
    }

    public static String GetResourceInputString(String resourceName)
    {
        var result = String.Empty;
        using ( var sr = new StreamReader(GetResourceInputStream(resourceName)) )
        {
            result = sr.ReadToEnd();
            sr.Close();
        }
        return result;
    }
}
