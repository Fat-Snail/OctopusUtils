namespace OctopusEx.WebCore.Tests.HealthChecks;

using Caching;
using Events;
using Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MultiTenancy;

public class HealthCheckRegistrationTests
{
    [Fact]
    public void CommonHealthChecks_ShouldNotDuplicateModuleRegistrations()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSimpleCache();
        builder.Services.AddSimpleEventBus();
        builder.Services.AddOutbox();
        builder.Services.AddSimpleMultiTenancy();

        builder.AddOctopusCacheHealthCheck();
        builder.AddEventBusHealthCheck();
        builder.AddOutboxHealthCheck();
        builder.AddTenantHealthCheck();
        builder.AddCommonHealthChecks();

        using WebApplication app = builder.Build();
        HealthCheckService healthCheckService = app.Services.GetRequiredService<HealthCheckService>();

        healthCheckService.Should().NotBeNull();
    }
}
