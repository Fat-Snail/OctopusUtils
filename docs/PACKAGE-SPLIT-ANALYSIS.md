# OctopusEx.WebCore 包结构拆分评估

> 状态：v1.5.3 评估，**暂不拆分**。本文记录决策依据，供后续重新评估参考。

## 现状

`OctopusEx.WebCore` 单包聚合：

| 模块 | 关键依赖 | 大小占比（粗估） |
|---|---|---|
| Core: DI / DomainCore / Helpers / Mapping | 框架内置 | 25% |
| Cache | `Microsoft.Extensions.Caching.*` | 5% |
| Auth (JWT) | `Microsoft.AspNetCore.Authentication.JwtBearer` | 10% |
| Hangfire | `Hangfire.*`（3 包） | 20% |
| AI 桥接 | `Octopus.Tools` (Microsoft.Extensions.AI) | 5% |
| Audit / Sensitive Words | `ToolGood.Words`、`Microsoft.SemanticKernel.Core` | 25% |
| Telemetry | `OpenTelemetry.*`（5 包） | 10% |

总传递依赖约 30+ 包。

## 候选拆分方案

```
OctopusEx.WebCore           — 仅 Core (DI + DomainCore + Helpers + Mapping + Exceptions + Middleware)
OctopusEx.WebCore.Cache     — ICacheService / Memory / MultiLevel / Distributed
OctopusEx.WebCore.Auth      — JWT / TokenService / ClaimsPrincipal 扩展
OctopusEx.WebCore.Hangfire  — HangfireExtensions / Filters
OctopusEx.WebCore.Telemetry — OTel + Aspire wire-in
OctopusEx.WebCore.SensitiveWords — ToolGood + SK 集成
OctopusEx.WebCore.MultiTenancy — IMultiTenant / Resolver / Filter
OctopusEx.WebCore.Events    — IEventBus / Outbox
```

## 反对意见（推迟拆分理由）

1. **当前用户基数小**：依赖体积痛点反馈尚未出现。"按需依赖"是优化，不是必需。
2. **维护成本翻倍**：8 个包 × 每次发版 = 8 倍 NuGet 元数据 / 版本号 / Release Notes 维护。
3. **跨包接口稳定性约束**：拆分后内部协作变成"公开 API"，每次微调可能 bump major version。
4. **Mono-package 模式被广泛接受**：MediatR、AutoMapper、Polly 等流行库长期单包，到一定规模再拆。
5. **命名空间 ≠ 包**：现有命名空间已经按功能分（`Caching`/`Auth`/`Events`），未来拆包是机械操作，无设计阻碍。

## 触发拆分的信号

满足任一即重新评估：

- [ ] 用户 issue 反馈"只想用 X 但被迫拖入 Hangfire/SK"
- [ ] 包体积超过 5MB（当前约 1.2MB）
- [ ] 总传递依赖超过 50 个
- [ ] 出现互斥使用模式（如同时不能用 cache 和 events）

## 短期改进（不拆包也能做）

- 把 `Microsoft.SemanticKernel.Core` 改为可选：`SensitiveWordFilterPlugin` 的 SK 路径已与 `IOctopusChatService` 路径并存（v1.4.0 完成）。可以考虑把 SK 包标为 `<PrivateAssets>none</PrivateAssets>` 不传递。
- `Hangfire.MemoryStorage` 仅 demo/test 用途，可以标注 `<PrivateAssets>contentfiles;analyzers</PrivateAssets>` 减少传递。
- 文档明确"按需安装"指引：哪些场景需要哪些 NuGet。

## 决策

**v1.5.x 保持单包。** 在出现明确触发信号前不拆。短期内做依赖标注优化（上一节）即可。
