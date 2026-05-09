namespace JiebaNet.Segmenter.Common;

public static class FileExtension
{
    private static String _embeddedRoot = "Jieba.Segmenter";

    public static String ReadEmbeddedAllLine(String path)
    {
        if ( File.Exists(path) )
            return File.ReadAllText(path);

        path = path.Replace("/", ".").Replace("\\", ".");
        return ResourceHelper.GetResourceInputString($"{_embeddedRoot}.{path}");
    }

    public static List<String> ReadEmbeddedAllLines(String path, Encoding encoding)
    {
        var assembly = typeof(FileExtension).GetTypeInfo().Assembly;
        return ReadEmbeddedAllLines(assembly, path, encoding);
    }

    public static List<String> ReadEmbeddedAllLines(String path)
    {
        return ReadEmbeddedAllLines(path, Encoding.UTF8);
    }

    public static List<String> ReadAllLines(String path)
    {
        return ReadAllLines(path, Encoding.UTF8);
    }

    public static List<String> ReadAllLines(String path, Encoding encoding)
    {
        var list = new List<String>();
        using var streamReader = new StreamReader(path, encoding);
        String? item;
        while ( (item = streamReader.ReadLine()) != null )
            list.Add(item);
        return list;
    }

    public static List<String> ReadEmbeddedAllLines(Assembly assembly, String path)
    {
        return ReadEmbeddedAllLines(assembly, path, Encoding.UTF8);
    }

    public static List<String> ReadEmbeddedAllLines(Assembly assembly, String path, Encoding encoding)
    {
        var text = ReadEmbeddedAllLine(path);
        var lines = System.Text.RegularExpressions.Regex.Split(text, @"\n");
        return new List<String>(lines);
    }
}
