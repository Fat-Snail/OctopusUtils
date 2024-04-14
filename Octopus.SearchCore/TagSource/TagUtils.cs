using System.Reflection;
using System.Text.RegularExpressions;

namespace Octopus.SearchCore.TagSource;

public class TagUtils
{
    internal static readonly Regex RegexUserDict =
        new Regex("^(?<word>.+?)(?<freq> [0-9]+)?(?<tag> [a-z]+)?$", RegexOptions.Compiled);

    private static string _tagRoleEmbeddedPath = "TagSource.tag_role.txt";
    private static string _tagSceneEmbeddedPath = "TagSource.tag_scene.txt";
    
    private static Assembly _asm = Assembly.GetExecutingAssembly();

    public static JiebaNet.Segmenter.JiebaSegmenter GetSegmenter()
    {
        var segmenter = new JiebaNet.Segmenter.JiebaSegmenter();
        segmenter.LoadUserDictFromText(GetResourceInputString(_tagRoleEmbeddedPath));
        segmenter.LoadUserDictFromText(GetResourceInputString(_tagSceneEmbeddedPath));

        return segmenter;
    }

    public static List<string> GetAllTags()
    {
        var tags = new List<string>();

        var text = GetResourceInputString(_tagRoleEmbeddedPath);
        tags.AddRange(GetTagsFormText(text));

        text = GetResourceInputString(_tagSceneEmbeddedPath);
        tags.AddRange(GetTagsFormText(text));
        
        return tags;
    }

    public static string GetAllTagsToJson()
    {
        var jsonObj = new { code = 1, msg = "success", data = new List<object>() };

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

    private static List<string> GetTagsFormText(string text)
    {
        var tags = new List<string>();

        var lines = text.Split(new[] { Environment.NewLine },
            StringSplitOptions.None
        );
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
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
    
    public static Stream GetResourceInputStream(string resourceName)
    {
        return _asm.GetManifestResourceStream(string.Format("{0}.{1}", _asm.GetName().Name, resourceName));
    }

    public static string GetResourceInputString(string resourceName)
    {
        string result = string.Empty;
        using (StreamReader sr = new StreamReader(GetResourceInputStream(resourceName)))
        {
            result = sr.ReadToEnd();
            sr.Close();
        }
        return result;
    } 
}