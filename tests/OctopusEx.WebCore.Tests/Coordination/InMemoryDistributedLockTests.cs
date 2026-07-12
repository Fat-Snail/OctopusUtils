namespace OctopusEx.WebCore.Tests.Coordination;

using OctopusEx.WebCore.Coordination;

public class InMemoryDistributedLockTests
{
    [Fact]
    public async Task SameKey_ShouldAllowOnlyOneHolder()
    {
        using var provider = new InMemoryDistributedLockProvider();
        await using var first = await provider.AcquireAsync("same", new DistributedLockOptions { LeaseTime = TimeSpan.FromSeconds(1) });

        await using var second = await provider.AcquireAsync("same", new DistributedLockOptions { WaitTime = TimeSpan.FromMilliseconds(20) });

        first.Acquired.Should().BeTrue();
        second.Acquired.Should().BeFalse();
    }

    [Fact]
    public async Task ExpiredLease_ShouldBeRecoverable()
    {
        using var provider = new InMemoryDistributedLockProvider();
        await using var first = await provider.AcquireAsync("recover", new DistributedLockOptions
        {
            LeaseTime = TimeSpan.FromMilliseconds(50),
            AutoRenew = false
        });

        await Task.Delay(80);
        await using var second = await provider.AcquireAsync("recover");

        first.Acquired.Should().BeTrue();
        second.Acquired.Should().BeTrue();
    }

    [Fact]
    public async Task Dispose_ShouldBeIdempotent_AndNotReleaseOtherOwner()
    {
        using var provider = new InMemoryDistributedLockProvider();
        var first = await provider.AcquireAsync("token");
        await first.DisposeAsync();
        await first.DisposeAsync();

        await using var second = await provider.AcquireAsync("token");
        second.Acquired.Should().BeTrue();
    }
}
