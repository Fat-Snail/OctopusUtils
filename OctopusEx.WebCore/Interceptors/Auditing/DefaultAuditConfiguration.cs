using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace OctopusEx.WebCore.Interceptors.Auditing;

public class DefaultAuditConfiguration : IAuditConfiguration
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly ConcurrentDictionary<string, DomainAuditConfiguration> _domainConfigurations;

    public DefaultAuditConfiguration() : this(null) { }

    public DefaultAuditConfiguration(IHttpContextAccessor? httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        _domainConfigurations = new ConcurrentDictionary<string, DomainAuditConfiguration>();

        _domainConfigurations.TryAdd("System", new DomainAuditConfiguration
        {
            Enabled = true,
            IgnoredProperties = new List<string> { "PasswordHash", "SecurityStamp" }
        });

        _domainConfigurations.TryAdd("Product", new DomainAuditConfiguration
        {
            Enabled = true,
            IgnoredProperties = new List<string> { "InternalCode", "LastPrice" }
        });

        _domainConfigurations.TryAdd("Audit", new DomainAuditConfiguration { Enabled = false });
    }

    public bool Enabled { get; set; } = true;

    public IReadOnlyCollection<string> GlobalIgnoredProperties { get; set; } = new List<string>
    {
        "CreatedAt",
        "UpdatedAt",
        "CreatedBy",
        "UpdatedBy",
        "Timestamp",
        "RowVersion"
    };

    public DomainAuditConfiguration GetDomainConfiguration(string domainName)
    {
        if ( _domainConfigurations.TryGetValue(domainName, out var cfg) ) return cfg;
        var defaultCfg = new DomainAuditConfiguration { Enabled = true };
        _domainConfigurations.TryAdd(domainName, defaultCfg);
        return defaultCfg;
    }

    public AuditUserInfo GetCurrentUser()
    {
        if ( _httpContextAccessor?.HttpContext != null )
        {
            var http = _httpContextAccessor.HttpContext;
            var user = http.User;
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name ?? "anonymous";
            var userName = user.Identity?.Name ?? userId;
            var ip = http.Connection?.RemoteIpAddress?.ToString();
            var ua = http.Request.Headers["User-Agent"].ToString();
            return new AuditUserInfo { UserId = userId, UserName = userName, IpAddress = ip, UserAgent = ua };
        }
        return new AuditUserInfo { UserId = "system", UserName = "System User" };
    }

    public void ConfigureDomain(string domainName, DomainAuditConfiguration configuration)
    {
        _domainConfigurations.AddOrUpdate(domainName, configuration, (k, old) => configuration);
    }

    public void DisableDomain(string domainName)
    {
        if ( _domainConfigurations.TryGetValue(domainName, out var cfg) )
        {
            cfg.Enabled = false;
        }
        else
        {
            _domainConfigurations.TryAdd(domainName, new DomainAuditConfiguration { Enabled = false });
        }
    }

    public void AddIgnoredProperties(string domainName, params string[] properties)
    {
        var config = _domainConfigurations.GetOrAdd(domainName, new DomainAuditConfiguration());
        var ignoredProps = new List<string>(config.IgnoredProperties ?? new List<string>());
        ignoredProps.AddRange(properties);
        config.IgnoredProperties = ignoredProps;
    }
}
