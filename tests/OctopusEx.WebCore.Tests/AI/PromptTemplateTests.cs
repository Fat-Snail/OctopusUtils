namespace OctopusEx.WebCore.Tests.AI;

using Octopus.AI;

public class PromptTemplateTests
{
    [Fact]
    public void Render_ReplacesPlaceholders_FromDictionary()
    {
        var t = new PromptTemplate("Hello {name}, you are {age} years old.");
        var result = t.Render(new Dictionary<String, Object?> { ["name"] = "Alice", ["age"] = 30 });
        result.Should().Be("Hello Alice, you are 30 years old.");
    }

    [Fact]
    public void Render_ReplacesPlaceholders_FromAnonymousObject()
    {
        var t = new PromptTemplate("Hello {name}");
        var result = t.Render(new { name = "Bob" });
        result.Should().Be("Hello Bob");
    }

    [Fact]
    public void Render_UnknownPlaceholder_RemainsLiteral()
    {
        var t = new PromptTemplate("Hello {name}, {unknown}");
        var result = t.Render(new { name = "X" });
        result.Should().Be("Hello X, {unknown}");
    }

    [Fact]
    public void Render_NullValue_RendersAsEmptyString()
    {
        var t = new PromptTemplate("[{x}]");
        var result = t.Render(new Dictionary<String, Object?> { ["x"] = null });
        result.Should().Be("[]");
    }

    [Fact]
    public void ImplicitConversion_FromString_Works()
    {
        PromptTemplate t = "Hi {x}";
        t.Render(new { x = "there" }).Should().Be("Hi there");
    }
}
