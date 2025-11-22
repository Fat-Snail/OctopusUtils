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

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

[MIT License](LICENSE)
