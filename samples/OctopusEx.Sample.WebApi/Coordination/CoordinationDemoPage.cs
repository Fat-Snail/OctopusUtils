namespace OctopusEx.Sample.WebApi.Coordination;

using System.Net;

internal static class CoordinationDemoPage
{
    public static String Render(CoordinationDemoResult result, String backend)
    {
        var rows = String.Join(String.Empty, result.Contenders.Select(item =>
            $"<tr><td>{WebUtility.HtmlEncode(item.Name)}</td><td>{(item.Acquired ? "已获取" : "未获取")}</td><td>{(item.LeaseLost ? "是" : "否")}</td><td>{(item.FinishedAt - item.StartedAt).TotalMilliseconds:F0} ms</td><td>{WebUtility.HtmlEncode(item.Message)}</td></tr>"));
        var html = """
<!doctype html><html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>分布式锁演示</title>
<style>body{{margin:0;background:#08111f;color:#dbeafe;font:16px system-ui,sans-serif}}main{{max-width:1000px;margin:0 auto;padding:48px 20px}}h1{{color:#f8fafc}}.lead{{color:#94a3b8}}.cards{{display:flex;gap:12px;flex-wrap:wrap;margin:24px 0}}.card{{background:#0f1d31;border:1px solid #1e3a5f;border-radius:12px;padding:16px 20px}}.label{{color:#7dd3fc;font-size:13px}}.value{{display:block;margin-top:6px;font-size:20px;font-weight:700}}table{{width:100%;border-collapse:collapse;background:#0b1728}}th,td{{padding:14px;text-align:left;border-bottom:1px solid #1e3a5f}}th{{color:#7dd3fc}}a{{color:#7dd3fc}}</style></head>
<body><main><p><a href="/">← 返回 Sample 首页</a></p><h1>🐙 分布式锁竞争演示</h1><p class="lead">两个并发任务竞争同一把租约锁，同一时刻只有一个任务可以进入临界区。</p><div class="cards"><div class="card"><span class="label">锁后端</span><span class="value">BACKEND</span></div><div class="card"><span class="label">锁 Key</span><span class="value">LOCKKEY</span></div><div class="card"><span class="label">竞争者</span><span class="value">COUNT</span></div></div><table><thead><tr><th>任务</th><th>结果</th><th>租约丢失</th><th>耗时</th><th>说明</th></tr></thead><tbody>ROWS</tbody></table><p class="lead">程序化调用：<a href="/coordination/demo?format=json">/coordination/demo?format=json</a></p></main></body></html>
""";
        return html.Replace("BACKEND", WebUtility.HtmlEncode(backend), StringComparison.Ordinal)
            .Replace("LOCKKEY", WebUtility.HtmlEncode(result.LockKey), StringComparison.Ordinal)
            .Replace("COUNT", result.Contenders.Count.ToString(), StringComparison.Ordinal)
            .Replace("ROWS", rows, StringComparison.Ordinal);
    }
}
