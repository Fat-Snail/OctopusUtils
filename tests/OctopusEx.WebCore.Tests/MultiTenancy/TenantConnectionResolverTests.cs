namespace OctopusEx.WebCore.Tests.MultiTenancy;

using OctopusEx.WebCore.MultiTenancy;

public class TenantConnectionResolverTests
{
    private static DictionaryTenantConnectionResolver Build(
        String? currentTenant,
        Dictionary<String, String> map,
        String? defaultConn = null)
    {
        var ct = new CurrentTenant();
        if (currentTenant != null) ct.Use(currentTenant);
        return new DictionaryTenantConnectionResolver(ct, map, defaultConn);
    }

    [Fact]
    public void Resolve_KnownTenant_ReturnsMapping()
    {
        var resolver = Build("acme", new() { ["acme"] = "Server=acme;..." });
        resolver.Resolve().Should().Contain("acme");
    }

    [Fact]
    public void Resolve_UnknownTenant_FallsBackToDefault()
    {
        var resolver = Build("missing", new() { ["x"] = "X" }, defaultConn: "Server=default");
        resolver.Resolve().Should().Be("Server=default");
    }

    [Fact]
    public void Resolve_NoTenantContext_NoDefault_Throws()
    {
        var resolver = Build(currentTenant: null, new());
        var act = () => resolver.Resolve();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Resolve_ByExplicitId_BypassesCurrentTenant()
    {
        var resolver = Build("acme", new() { ["other"] = "Server=other" });
        resolver.Resolve("other").Should().Be("Server=other");
    }
}
