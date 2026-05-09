internal class ResourceHelper
{
    private static readonly Assembly Asm = Assembly.GetExecutingAssembly();
    private static readonly String AsmName = Asm.GetName().Name;
    private static readonly String[] ResourceNames = Asm.GetManifestResourceNames();

    public static Stream? GetResourceInputStream(String resourceName)
    {
        var exact = Asm.GetManifestResourceStream($"{AsmName}.{resourceName}");
        if (exact != null) return exact;

        // AssemblyName ≠ RootNamespace 时后缀匹配兜底
        var match = ResourceNames.FirstOrDefault(n => n.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));
        return match != null ? Asm.GetManifestResourceStream(match) : null;
    }

    public static String GetResourceInputString(String resourceName)
    {
        using var sr = new StreamReader(GetResourceInputStream(resourceName)!);
        return sr.ReadToEnd();
    }
}
