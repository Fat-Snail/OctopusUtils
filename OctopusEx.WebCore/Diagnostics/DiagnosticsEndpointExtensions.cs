namespace OctopusEx.WebCore.Diagnostics;

using System.Reflection;
using System.Text;
using System.Text.Json;
using Events;
using Events.Outbox;
using Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MultiTenancy;

/// <summary>
/// 诊断端点扩展。提供 /octopus/diagnostics 端点，输出运行时刻关键状态信息。
/// Development 环境默认开启；Production 可通过 configure 手动开启并添加授权。
/// </summary>
public static class DiagnosticsEndpointExtensions
{
    /// <summary>
    /// 映射 Octopus 诊断端点到 /octopus/diagnostics。
    /// Development 下自动开启，其他环境需显式调用。
    /// </summary>
    /// <param name="app">WebApplication</param>
    /// <param name="requireAuthorization">是否需要授权。Development 默认 false。</param>
    public static IEndpointRouteBuilder MapOctopusDiagnostics(this IEndpointRouteBuilder endpoints, Boolean requireAuthorization = false)
    {
        var route = endpoints.MapGet("/octopus/diagnostics", async (HttpContext http) =>
        {
            var result = await BuildDiagnosticsReportAsync(http);

            var accept = http.Request.Headers.Accept.FirstOrDefault();
            if (accept != null && accept.Contains("text/html", StringComparison.OrdinalIgnoreCase))
                return Results.Content(RenderHtml(result), "text/html; charset=utf-8");

            return Results.Json(result, new JsonSerializerOptions { WriteIndented = true });
        });

        if (requireAuthorization)
            route.RequireAuthorization();

        route.WithTags("Diagnostics");
        return endpoints;
    }

    private static async Task<DiagnosticsReport> BuildDiagnosticsReportAsync(HttpContext http)
    {
        var report = new DiagnosticsReport
        {
            Timestamp = DateTimeOffset.UtcNow,
            MachineName = Environment.MachineName,
            AppVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown"
        };

        // --- 缓存命中率 ---
        var cacheService = http.RequestServices.GetService<Caching.ICacheService>();
        if (cacheService != null)
        {
            report.Cache = new CacheDiagnostics
            {
                Type = cacheService.GetType().Name,
                Connected = true
            };

            try
            {
                await cacheService.SetAsync("__diag__", "ok", TimeSpan.FromSeconds(2));
                var cr = await cacheService.TryGetAsync<String>("__diag__");
                await cacheService.RemoveAsync("__diag__", CancellationToken.None);
                report.Cache.ReadWriteOk = cr is { Found: true, Value: "ok" };
            }
            catch
            {
                report.Cache.ReadWriteOk = false;
            }
        }
        else
        {
            report.Cache = new CacheDiagnostics { Connected = false };
        }

        // --- Outbox 积压 ---
        var outboxStore = http.RequestServices.GetService<IOutboxStore>();
        if (outboxStore != null)
        {
            try
            {
                var pending = await outboxStore.FetchPendingAsync(1000, Int32.MaxValue);
                report.Outbox = new OutboxDiagnostics
                {
                    PendingCount = pending.Count,
                    OldestPending = pending.MinBy(m => m.CreatedAt)?.CreatedAt,
                    RecentErrors = pending.Where(m => m.LastError != null).Take(5)
                        .Select(m => new OutboxErrorSummary { EventType = m.EventType, Attempts = m.AttemptCount, Error = m.LastError! })
                        .ToList()
                };
            }
            catch (Exception ex)
            {
                report.Outbox = new OutboxDiagnostics { PendingCount = null, Error = ex.Message };
            }
        }

        // --- 死信队列 ---
        var deadLetterStore = http.RequestServices.GetService<IDeadLetterStore>();
        if (deadLetterStore != null)
        {
            try
            {
                var letters = await deadLetterStore.ListAsync(50);
                report.DeadLetters = new DeadLetterDiagnostics
                {
                    TotalCount = letters.Count,
                    Recent = letters.Take(10).Select(d => new DeadLetterSummary
                    {
                        EventId = d.EventId,
                        EventType = d.EventTypeName,
                        Handler = d.HandlerTypeName,
                        Error = d.ErrorMessage,
                        FailedAt = d.FailedAt
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                report.DeadLetters = new DeadLetterDiagnostics { TotalCount = null, Error = ex.Message };
            }
        }

        // --- 当前用户 / 租户 ---
        var currentUser = http.RequestServices.GetService<ICurrentUser>();
        var currentTenant = http.RequestServices.GetService<ICurrentTenant>();

        report.Identity = new IdentityDiagnostics
        {
            UserId = currentUser?.UserId,
            UserName = currentUser?.UserName,
            IsAuthenticated = currentUser?.IsAuthenticated ?? false,
            TenantId = currentTenant?.TenantId
        };

        return report;
    }

    private static String RenderHtml(DiagnosticsReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html><html lang=\"en\"><head>");
        sb.AppendLine("<meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
        sb.AppendLine("<title>Octopus Diagnostics</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:system-ui,-apple-system,sans-serif;margin:2rem;background:#0d1117;color:#c9d1d9;}");
        sb.AppendLine("h1{color:#58a6ff;}h2{color:#f0883e;margin-top:1.5rem;}");
        sb.AppendLine(".card{background:#161b22;border:1px solid #30363d;border-radius:6px;padding:1rem;margin:0.5rem 0;}");
        sb.AppendLine("table{width:100%;border-collapse:collapse;}td,th{padding:0.4rem 0.8rem;text-align:left;border-bottom:1px solid #30363d;}");
        sb.AppendLine("th{color:#8b949e;font-weight:600;}.ok{color:#3fb950;}.warn{color:#d29922;}.err{color:#f85149;}");
        sb.AppendLine("pre{background:#0d1117;padding:0.5rem;border-radius:4px;overflow-x:auto;font-size:0.85em;}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine($"<h1>Octopus Diagnostics</h1><p>{report.Timestamp:O} | {report.MachineName} | v{report.AppVersion}</p>");

        // Cache
        sb.AppendLine("<h2>Cache</h2><div class=\"card\">");
        sb.AppendFormat("<p>Type: <strong>{0}</strong> | Connected: <span class=\"{1}\">{2}</span> | Read/Write: <span class=\"{3}\">{4}</span></p>",
            report.Cache.Type,
            report.Cache.Connected ? "ok" : "err", report.Cache.Connected ? "yes" : "no",
            report.Cache.ReadWriteOk == true ? "ok" : "err", report.Cache.ReadWriteOk == true ? "ok" : "fail");
        sb.AppendLine("</div>");

        // Outbox
        sb.AppendLine("<h2>Outbox</h2><div class=\"card\">");
        if (report.Outbox.Error != null)
            sb.AppendFormat("<p class=\"err\">Error: {0}</p>", Escape(report.Outbox.Error));
        else
        {
            var cls = report.Outbox.PendingCount switch
            {
                null => "warn",
                <= 100 => "ok",
                <= 500 => "warn",
                _ => "err"
            };
            sb.AppendFormat("<p>Pending: <strong class=\"{0}\">{1}</strong></p>", cls, report.Outbox.PendingCount ?? -1);
            if (report.Outbox.OldestPending.HasValue)
                sb.AppendFormat("<p>Oldest pending: {0:O}</p>", report.Outbox.OldestPending);
            if (report.Outbox.RecentErrors.Count > 0)
            {
                sb.AppendLine("<table><tr><th>Event</th><th>Attempts</th><th>Error</th></tr>");
                foreach (var e in report.Outbox.RecentErrors)
                    sb.AppendFormat("<tr><td>{0}</td><td>{1}</td><td><pre>{2}</pre></td></tr>", Escape(e.EventType), e.Attempts, Escape(e.Error));
                sb.AppendLine("</table>");
            }
        }
        sb.AppendLine("</div>");

        // Dead Letters
        sb.AppendLine("<h2>Dead Letters</h2><div class=\"card\">");
        if (report.DeadLetters.Error != null)
            sb.AppendFormat("<p class=\"err\">Error: {0}</p>", Escape(report.DeadLetters.Error));
        else
        {
            sb.AppendFormat("<p>Count: <strong>{0}</strong></p>", report.DeadLetters.TotalCount);
            if (report.DeadLetters.Recent.Count > 0)
            {
                sb.AppendLine("<table><tr><th>EventId</th><th>Type</th><th>Handler</th><th>Error</th></tr>");
                foreach (var d in report.DeadLetters.Recent)
                    sb.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td><pre>{3}</pre></td></tr>",
                        d.EventId.ToString()[..8], Escape(d.EventType), Escape(d.Handler), Escape(d.Error));
                sb.AppendLine("</table>");
            }
        }
        sb.AppendLine("</div>");

        // Identity
        sb.AppendLine("<h2>Identity</h2><div class=\"card\">");
        sb.AppendFormat("<p>User: <strong>{0}</strong> ({1}) | Authenticated: <span class=\"{2}\">{3}</span></p>",
            Escape(report.Identity.UserName ?? "(anonymous)"), Escape(report.Identity.UserId ?? "-"),
            report.Identity.IsAuthenticated ? "ok" : "warn", report.Identity.IsAuthenticated);
        sb.AppendFormat("<p>Tenant: <strong>{0}</strong></p>", Escape(report.Identity.TenantId ?? "(none)"));
        sb.AppendLine("</div>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static String Escape(String? s) => System.Net.WebUtility.HtmlEncode(s ?? "");
}

// ---- 内部 DTO ----

internal class DiagnosticsReport
{
    public DateTimeOffset Timestamp { get; set; }
    public String MachineName { get; set; } = "";
    public String AppVersion { get; set; } = "";
    public CacheDiagnostics Cache { get; set; } = new();
    public OutboxDiagnostics Outbox { get; set; } = new();
    public DeadLetterDiagnostics DeadLetters { get; set; } = new();
    public IdentityDiagnostics Identity { get; set; } = new();
}

internal class CacheDiagnostics
{
    public String Type { get; set; } = "";
    public Boolean Connected { get; set; }
    public Boolean? ReadWriteOk { get; set; }
}

internal class OutboxDiagnostics
{
    public Int32? PendingCount { get; set; }
    public DateTimeOffset? OldestPending { get; set; }
    public List<OutboxErrorSummary> RecentErrors { get; set; } = new();
    public String? Error { get; set; }
}

internal class OutboxErrorSummary
{
    public String EventType { get; set; } = "";
    public Int32 Attempts { get; set; }
    public String Error { get; set; } = "";
}

internal class DeadLetterDiagnostics
{
    public Int32? TotalCount { get; set; }
    public List<DeadLetterSummary> Recent { get; set; } = new();
    public String? Error { get; set; }
}

internal class DeadLetterSummary
{
    public Guid EventId { get; set; }
    public String EventType { get; set; } = "";
    public String Handler { get; set; } = "";
    public String Error { get; set; } = "";
    public DateTimeOffset FailedAt { get; set; }
}

internal class IdentityDiagnostics
{
    public String? UserId { get; set; }
    public String? UserName { get; set; }
    public Boolean IsAuthenticated { get; set; }
    public String? TenantId { get; set; }
}
