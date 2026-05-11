namespace OctopusEx.Aspire.Tests;

using System.Reflection;
using System.Text.Json;

public class RemoteKvFlattenTests
{
    /// <summary>
    /// 直接测试内部 FlattenInto，避免起一个真 HTTP 服务器。
    /// </summary>
    private static IDictionary<String, String?> Flatten(String json)
    {
        var providerType = typeof(OctopusEx.Aspire.AspireOctopusWiring).Assembly
            .GetType("OctopusEx.Aspire.RemoteKvConfigProvider")!;
        var method = providerType.GetMethod("FlattenInto", BindingFlags.Static | BindingFlags.NonPublic)!;

        using var doc = JsonDocument.Parse(json);
        var data = new Dictionary<String, String?>();
        method.Invoke(null, new Object[] { doc.RootElement, "", data });
        return data;
    }

    [Fact]
    public void Flatten_FlatStringValues()
    {
        var data = Flatten("""{"foo":"a","bar":"b"}""");
        data["foo"].Should().Be("a");
        data["bar"].Should().Be("b");
    }

    [Fact]
    public void Flatten_NestedObject_UsesColonSeparator()
    {
        var data = Flatten("""{"db":{"host":"localhost","port":5432}}""");
        data["db:host"].Should().Be("localhost");
        data["db:port"].Should().Be("5432");
    }

    [Fact]
    public void Flatten_Array_UsesIndexedKeys()
    {
        var data = Flatten("""{"servers":["a","b","c"]}""");
        data["servers:0"].Should().Be("a");
        data["servers:1"].Should().Be("b");
        data["servers:2"].Should().Be("c");
    }

    [Fact]
    public void Flatten_BooleansAndNumbers_ConvertedToString()
    {
        var data = Flatten("""{"enabled":true,"timeout":30}""");
        data["enabled"].Should().Be("True");
        data["timeout"].Should().Be("30");
    }

    [Fact]
    public void Flatten_Null_PreservedAsNullValue()
    {
        var data = Flatten("""{"empty":null}""");
        data.ContainsKey("empty").Should().BeTrue();
        data["empty"].Should().BeNull();
    }

    [Fact]
    public void Flatten_DeeplyNested_StillCorrect()
    {
        var data = Flatten("""{"a":{"b":{"c":{"d":"deep"}}}}""");
        data["a:b:c:d"].Should().Be("deep");
    }
}
