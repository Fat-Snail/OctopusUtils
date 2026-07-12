namespace OctopusEx.Sample.WebApi;

internal static class SampleLandingPage
{
    internal const String Html = """
        <!doctype html>
        <html lang="zh-CN">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>OctopusEx WebApi Sample</title>
          <style>
            :root { color-scheme: dark; font-family: Inter, ui-sans-serif, system-ui, sans-serif; }
            * { box-sizing: border-box; }
            body { margin: 0; background: #08111f; color: #dbeafe; }
            main { width: min(1040px, calc(100% - 32px)); margin: 0 auto; padding: 64px 0; }
            .eyebrow { color: #38bdf8; font-weight: 700; letter-spacing: .12em; text-transform: uppercase; }
            h1 { margin: 12px 0 16px; font-size: clamp(38px, 7vw, 72px); line-height: 1; color: #f8fafc; }
            .lead { max-width: 760px; color: #94a3b8; font-size: 18px; line-height: 1.7; }
            .actions { display: flex; flex-wrap: wrap; gap: 12px; margin: 28px 0 48px; }
            a.button { padding: 11px 16px; border: 1px solid #334155; border-radius: 10px; color: #e0f2fe; text-decoration: none; background: #0f1d31; }
            a.button.primary { color: #082f49; background: #38bdf8; border-color: #38bdf8; font-weight: 700; }
            .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 16px; }
            section { padding: 22px; border: 1px solid #1e3a5f; border-radius: 14px; background: #0b1728; }
            h2 { margin: 0 0 10px; color: #f8fafc; font-size: 18px; }
            p, li { color: #94a3b8; line-height: 1.6; }
            ul { padding-left: 20px; margin-bottom: 0; }
            code { color: #7dd3fc; background: #07101d; padding: 2px 6px; border-radius: 5px; }
            pre { overflow-x: auto; margin: 12px 0 0; padding: 14px; border-radius: 10px; background: #050b14; color: #bae6fd; line-height: 1.55; }
            footer { margin-top: 32px; color: #64748b; }
          </style>
        </head>
        <body>
          <main>
            <div class="eyebrow">OctopusUtils · v1.5.5 Sample</div>
            <h1>WebCore 能力展示</h1>
            <p class="lead">这是 OctopusEx.WebCore 的可运行示例，串联多租户、JWT、EF Core、缓存、事件总线、Outbox、Hangfire、审计、健康检查与诊断能力。</p>
            <div class="actions">
              <a class="button primary" href="/health">查看服务状态</a>
              <a class="button" href="/health/full">完整健康检查</a>
              <a class="button" href="/octopus/diagnostics">诊断面板</a>
              <a class="button" href="/openapi/v1.json">OpenAPI JSON</a>
            </div>
            <div class="grid">
              <section>
                <h2>示例包含什么</h2>
                <ul>
                  <li>基于 Header 的多租户隔离</li>
                  <li>JWT 认证与当前用户上下文</li>
                  <li>SQLite + 软删除 + Mapster</li>
                  <li>事件总线、Outbox 与后台任务</li>
                </ul>
              </section>
              <section>
                <h2>健康与诊断</h2>
                <ul>
                  <li><code>/health/live</code>：存活检查</li>
                  <li><code>/health/ready</code>：就绪检查</li>
                  <li><code>/health/full</code>：全部检查</li>
                  <li><code>/octopus/diagnostics</code>：开发环境诊断页</li>
                </ul>
              </section>
              <section>
                <h2>调用 Todo API</h2>
                <p>Todo 接口需要 JWT，并通过 <code>X-Tenant-Id</code> 区分租户。</p>
                <pre>curl http://localhost:5000/api/todos \
          -H "Authorization: Bearer &lt;token&gt;" \
          -H "X-Tenant-Id: tenant-a"</pre>
              </section>
            </div>
            <footer>提示：本 Sample 始终开放 OpenAPI 与诊断页；实际生产项目应按环境限制 OpenAPI，并为诊断端点启用授权。</footer>
          </main>
        </body>
        </html>
        """;
}
