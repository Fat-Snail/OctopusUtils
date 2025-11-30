# Octopus
Octopus组件库，聚合一些有用的代码、工具类

## 聚合全文索引工具类

- 全文搜索（Lucene.NET 4.8.0-beta00017）
- Tag分类提取
- 中文分词（基于结巴，后期会考虑加入盘古）

[使用说明](Search.md)

## 谷歌云盘下载工具类

- 免调用复杂的谷歌云盘API
- 简单易用，支持大文件 ~_~

[使用说明](Google.md)

## 简单控制台进度条

- 简单好用
- 使用控制台程序进行大规模运算可以让界面显示进度，避免等待

[使用说明](ConsoleShow.md)

## 异步彩色控制台输出 (ConsoleEx)

- 🎨 支持彩色控制台输出，提升应用界面美观度
- ⚡ 异步非阻塞写入，提高应用性能
- 🔄 优雅关闭机制，确保所有消息正确处理
- 📝 完整的XML文档注释，支持智能提示

### 基本使用示例

```csharp
using Octopus;

// 基本彩色输出
ConsoleEx.WriteLine("成功消息", ConsoleColor.Green);
ConsoleEx.WriteLine("警告消息", ConsoleColor.Yellow);
ConsoleEx.WriteLine("错误消息", ConsoleColor.Red);
ConsoleEx.WriteLine("信息消息", ConsoleColor.Blue);

// 不换行输出
ConsoleEx.Write("处理中... ", ConsoleColor.Cyan);
ConsoleEx.WriteLine("完成!", ConsoleColor.Green);

// 应用程序结束时优雅关闭异步控制台
await ConsoleEx.ShutdownAsync();
```

### 日志级别输出（带时间戳）

```csharp
using Octopus;

// 带时间戳的日志输出
ConsoleEx.Info("应用程序启动成功");
ConsoleEx.Debug("调试信息：用户ID = 12345");
ConsoleEx.Warn("磁盘空间不足，请及时清理");
ConsoleEx.Error("数据库连接失败，请检查配置");

// 输出示例：
// 2023-10-27 14:30:15 [INFO] 应用程序启动成功
// 2023-10-27 14:30:16 [DEBUG] 调试信息：用户ID = 12345
// 2023-10-27 14:30:17 [WARN] 磁盘空间不足，请及时清理
// 2023-10-27 14:30:18 [ERROR] 数据库连接失败，请检查配置
```

### 高级用法

```csharp
using Octopus;

// 批量彩色输出示例
var colors = new[] { 
    ConsoleColor.Red, ConsoleColor.Yellow, ConsoleColor.Green, 
    ConsoleColor.Blue, ConsoleColor.Magenta, ConsoleColor.Cyan 
};

var messages = new[] { "错误", "警告", "成功", "信息", "提示", "处理中" };

for (int i = 0; i < messages.Length; i++)
{
    ConsoleEx.WriteLine($"[{messages[i]}] 这是一条{messages[i]}消息", colors[i]);
    await Task.Delay(500); // 模拟处理过程
}

ConsoleEx.WriteLine("所有消息处理完成", ConsoleColor.White);

// 应用程序退出前等待所有消息写入完成
await ConsoleEx.ShutdownAsync(timeout: 3000);
```

### 性能优势

- **非阻塞输出**：控制台写入操作在独立线程中执行，不阻塞主线程
- **队列缓冲**：内置消息队列，支持高频率输出场景
- **异常安全**：内置异常处理，确保应用稳定性
- **资源管理**：支持优雅关闭，避免资源泄露

## 智能重试机制 (Utils.RetryMethod)

- 🔄 自动重试失败的操作，提高应用稳定性
- ⚡ 支持同步和异步操作
- 🔧 可配置重试次数、间隔时间
- 📊 提供重试回调，实时监控重试过程
- ✅ 异常安全，失败时提供详细错误信息

### 有返回值的方法重试

```csharp
using Octopus;

// 基本重试 - 返回结果
var result = Utils.RetryMethod(() => 
{
    // 可能会失败的操作
    return SomeMethodThatMightFail();
});

// 带重试回调
var data = Utils.RetryMethod(() => 
{
    return DownloadDataFromRemote();
}, 
maxRetryCount: 5, 
sleepTime: 500,
onRetry: (attempt, ex) => 
{
    ConsoleEx.Warn($"第{attempt}次重试失败: {ex.Message}");
});

// 失败时抛出异常
try
{
    var criticalResult = Utils.RetryMethod(() => 
    {
        return CriticalOperation();
    }, throwOnFailure: true);
}
catch (Exception ex)
{
    ConsoleEx.Error(ex);
}
```

### 无返回值的方法重试

```csharp
using Octopus;

// 基本重试 - 无返回值
Utils.RetryMethod(() => 
{
    // 可能会失败的操作
    SomeActionThatMightFail();
});

// 带重试回调的重试
Utils.RetryMethod(() => 
{
    UploadFileToServer();
}, 
maxRetryCount: 3,
onRetry: (attempt, ex) => 
{
    ConsoleEx.Info($"正在重试第{attempt}次...");
});

// 异步操作重试
await Utils.RetryMethodAsync(async () => 
{
    await ProcessDataAsync();
});

// 异步操作带返回值
var asyncResult = await Utils.RetryMethodAsync(async () => 
{
    return await FetchDataFromApiAsync();
});
```

### 使用场景

- **网络请求**：HTTP请求失败时自动重试
- **数据库操作**：连接失败时重试连接
- **文件操作**：文件被占用时等待重试
- **外部服务**：调用第三方服务失败时重试
- **资源竞争**：处理多线程环境下的资源竞争

## AI客户端（支持OpenAI、Llama）

- 简易的AI交互客户端
- 支持对话，简易发起会话，获取AI返回的数据
- 搭配本地Llama模型，可实现批量操作，譬如翻译任何一个国家语言，自动写（新闻、小说等），自动发稿

[使用说明](AIClient.md)

## 单元测试工具类

- 🔥 新增轻量级单元测试框架
- 📊 内置性能分析工具 MiniProfiler
- ✅ 简洁的断言工具 Assert
- 📝 支持测试报告格式化输出
- 🚀 专为 .NET 项目设计的测试工具

[使用说明](UnitTest.md)

---

## 📦 安装

```bash
git clone https://github.com/your-repo/OctopusUtils.git
cd OctopusUtils
dotnet restore
dotnet build
```

## 📖 文档

- [更新日志](CHANGELOG.md)
- [单元测试指南](UnitTest.md)
- [搜索功能使用](Search.md)
- [AI客户端使用](AIClient.md)
- [谷歌云盘下载](Google.md)
- [控制台进度条](ConsoleShow.md)
- **异步彩色控制台输出 (ConsoleEx)** - 内置文档注释和智能提示支持

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

[MIT License](LICENSE)
