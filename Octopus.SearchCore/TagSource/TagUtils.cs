namespace Octopus.SearchCore.TagSource;

public class TagUtils
{
    internal static readonly Regex RegexUserDict =
        new Regex("^(?<word>.+?)(?<freq> [0-9]+)?(?<tag> [a-z]+)?$", RegexOptions.Compiled);

    private static String _tagRoleEmbeddedPath = "TagSource.tag_role.txt";
    private static String _tagSceneEmbeddedPath = "TagSource.tag_scene.txt";

    private static readonly Assembly _asm = Assembly.GetExecutingAssembly();
    private static readonly String _asmName = _asm.GetName().Name;
    private static readonly String[] _resourceNames = _asm.GetManifestResourceNames();

    public static JiebaNet.Segmenter.JiebaSegmenter GetSegmenter()
    {
        var segmenter = new JiebaNet.Segmenter.JiebaSegmenter();
        segmenter.LoadUserDictFromText(GetResourceInputString(_tagRoleEmbeddedPath));
        segmenter.LoadUserDictFromText(GetResourceInputString(_tagSceneEmbeddedPath));

        return segmenter;
    }

    public static List<String> GetAllTags()
    {
        var tags = new List<String>();

        var text = GetResourceInputString(_tagRoleEmbeddedPath);
        tags.AddRange(GetTagsFormText(text));

        text = GetResourceInputString(_tagSceneEmbeddedPath);
        tags.AddRange(GetTagsFormText(text));

        return tags;
    }

    public static String GetAllTagsToJson()
    {
        var jsonObj = new { code = 1, msg = "success", data = new List<Object>() };

        var text = GetResourceInputString(_tagRoleEmbeddedPath);
        jsonObj.data.Add(
            new
            {
                role = GetTagsFormText(text)
            });

        text = GetResourceInputString(_tagSceneEmbeddedPath);
        jsonObj.data.Add(
            new
            {
                scene = GetTagsFormText(text)
            });

        return System.Text.Json.JsonSerializer.Serialize(jsonObj);
    }

    private static List<String> GetTagsFormText(String text)
    {
        var tags = new List<String>();

        var lines = text.Split(new[] { "\r\n", "\n" },
            StringSplitOptions.None
        );
        foreach ( var line in lines )
        {
            if ( String.IsNullOrWhiteSpace(line) )
            {
                continue;
            }

            var tokens = RegexUserDict.Match(line.Trim()).Groups;
            var word = tokens["word"].Value.Trim();
            var freq = tokens["freq"].Value.Trim();
            var tag = tokens["tag"].Value.Trim();

            tags.Add(word);
        }

        return tags;
    }

    private static Stream? GetResourceInputStream(String resourceName)
    {
        var exact = _asm.GetManifestResourceStream($"{_asmName}.{resourceName}");
        if (exact != null) return exact;

        // AssemblyName ≠ RootNamespace 时后缀匹配兜底
        var match = _resourceNames.FirstOrDefault(n => n.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));
        return match != null ? _asm.GetManifestResourceStream(match) : null;
    }

    private static String GetResourceInputString(String resourceName)
    {
        using var sr = new StreamReader(GetResourceInputStream(resourceName)!);
        return sr.ReadToEnd();
    }
}
