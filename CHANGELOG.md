# 更新日志

## [1.2.0] - 2025-12-20

### 新增
- ✨ **OctopusEx.WebCore** - ASP.NET Core Web 应用脚手架
  - 🎯 **ApiUIExtensions** - Swagger UI 和 Scalar UI 集成扩展
  - 🔍 **AspireExtensions** - .NET Aspire 链路追踪简化配置
  - 📊 **AuditServiceExtensions** - 基于领域的数据库审计系统
  - ⚡ **HangfireExtensions** - 后台作业调度扩展（支持单任务执行）
    - 🆕 支持 appsettings.json 配置 Dashboard 用户名密码
    - 🆕 内置 `HangfireAuthorizationFilter` 认证过滤器
  - 🔧 **HostBuilderExtensions** - 自动依赖注入脚手架
  - 📝 **完整示例项目** - 包含前后端代码示例

### 工具和配置
- ✨ 更新 **.editorconfig** - 支持 .NET 10 最新语法糖
- ✨ 配置 **Husky.NET** - 提交时自动格式化代码
- ✨ 支持 **.NET 10.0** - 升级 SDK 版本到 10.0.100

### 新增特性详情
- **ApiUIExtensions** - 支持 Swagger 和 Scalar UI 灵活切换
- **AspireExtensions** - 简化 OpenTelemetry 配置
- **AuditServiceExtensions** - 基于模型的可配置审计系统
- **HangfireExtensions** - 简化后台作业配置，支持单任务执行
  - `AddSimpleHangfire()` - 简化 Hangfire 配置
  - `AddRecurringJob()` - 添加定时作业
  - `AddBackgroundJob()` - 添加一次性作业
  - `UseHangfireDashboard()` - 配置 Dashboard 认证
- **自动依赖注入** - 基于接口的智能服务注册

### 更新 (2025-12-20)
- 📝 更新 **OctopusEx.WebCore/README.md** - 新增 Hangfire Dashboard 认证配置说明
- 📝 添加 appsettings.json 配置示例
- 📝 完善 Hangfire 扩展文档

---

## [1.1.0] - 2024-11-19

### 新增
- ✨ 添加单元测试工具类 UnitTest
- ✨ 新增性能分析工具 MiniProfiler 支持
- ✨ 添加测试断言工具 Assert
- ✨ 新增测试报告格式化功能

### 优化  
- 🔧 将所有基本类型升级为完整的 .NET 类型名称
- 🔧 string → String
- 🔧 int → Int32  
- 🔧 bool → Boolean
- 🔧 long → Int64
- 🔧 float → Single
- 🔧 double → Double
- 🔧 object → Object
- 🔧 char → Char

### 修复
- 🐛 修复 Rider IDE 临时文件被 Git 跟踪的问题
- 🐛 更新 .gitignore 忽略 .idea/ 目录
- 🐛 修复 .NET SDK 版本兼容性问题

---

## [1.0.0] - 2024-11-18

### 新增
- ✨ 聚合全文索引工具类（基于 Lucene.NET 4.8.0）
- ✨ Tag分类提取功能
- ✨ 中文分词功能（基于结巴分词）
- ✨ 谷歌云盘下载工具类
- ✨ 简单控制台进度条
- ✨ AI客户端（支持OpenAI、Llama）
- ✨ 支持对话交互和批量操作功能

---

> **版本规则**：遵循 [语义化版本 2.0.0](https://semver.org/lang/zh-CN/)
> **更新日志**：遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)