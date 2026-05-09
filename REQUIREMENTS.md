# OctopusUtils 需求文档

> 版本：基于代码库 v1.2.3 反向推导  
> 整理日期：2026-05-09  
> 作者：Fatty Coder

---

## 目录

1. [项目背景与目标](#1-项目背景与目标)
2. [模块一：Octopus.Tools — 核心工具库](#2-模块一octopustools--核心工具库)
3. [模块二：Octopus.Segment — 中文分词](#3-模块二octopussegment--中文分词)
4. [模块三：Octopus.SearchCore — 全文搜索引擎](#4-模块三octopussearchcore--全文搜索引擎)
5. [模块四：OctopusEx.WebCore — Web 脚手架](#5-模块四octopusexwebcore--web-脚手架)
6. [跨模块集成需求](#6-跨模块集成需求)
7. [非功能性需求](#7-非功能性需求)

---

## 1. 项目背景与目标

### 1.1 背景

中文互联网应用开发中存在大量重复的基础工作：控制台调试输出不支持异步、全文搜索缺乏中文友好支持、Web 项目反复搭建 CRUD 脚手架、依赖注入手动注册繁琐。OctopusUtils 的目标是将这些高频基础设施封装为开箱即用的 NuGet 组件库。

### 1.2 目标用户

- .NET 后端开发者（搜索/分词场景）
- ASP.NET Core 项目初始化（CRUD 脚手架、健康检查、审计）
- 需要批量 AI 处理的脚本开发者（控制台工具 + AI 客户端）

### 1.3 整体原则

- **零配置优先**：合理默认值，最小配置即可运行
- **渐进增强**：每个模块独立可用，也可组合使用
- **中文友好**：分词、搜索默认支持中文
- **强类型安全**：使用完整 .NET 类型名（`String`/`Int32`/`Boolean`），全程 Nullable 感知

---

## 2. 模块一：Octopus.Tools — 核心工具库

> 目标框架：netstandard2.0

### 2.1 异步彩色控制台（ConsoleEx / AsyncConsole）

#### 功能描述

提供非阻塞的彩色控制台输出能力，避免高并发场景下控制台 I/O 阻塞主线程。

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| T-CON-01 | 支持彩色文本输出 | `Write(message, ConsoleColor)` 与 `WriteLine(message, ConsoleColor)` |
| T-CON-02 | 内置日志级别快捷方法 | `Info`（绿色）、`Debug`（蓝色）、`Warn`（黄色）、`Error`（红色），均附带 `yyyy-MM-dd HH:mm:ss [LEVEL]` 时间戳前缀 |
| T-CON-03 | 异常对象直接输出 | `Error(Exception)` 方法，输出 `Message` + 换行 |
| T-CON-04 | 非阻塞写入 | 所有写操作投入 `BlockingCollection<ConsoleMessage>` 队列，由后台独立消费线程处理，不阻塞调用方 |
| T-CON-05 | 颜色状态还原 | 每条消息写入后，自动还原 `Console.ForegroundColor` 至写入前的颜色 |
| T-CON-06 | 优雅关闭 | `ShutdownAsync(timeout = 5000)` 停止接受新消息，等待队列清空或超时后强制取消 |
| T-CON-07 | 控制台重定向检测 | 进度条类检测 `Console.IsOutputRedirected`，重定向时禁用 UI 输出 |

#### 约束条件

- 队列满时不阻塞调用方（应使用有界或无界队列，超出容量时静默丢弃或等待）
- `ShutdownAsync` 超时后必须能正常返回，不抛出未处理异常

---

### 2.2 控制台进度条（ConsoleProgressBar）

#### 功能描述

在控制台程序中显示带动画效果的进度条，适用于大规模批量计算场景。

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| T-PRG-01 | 进度值报告 | 实现 `IProgress<Double>`，接受 `0.0 ~ 1.0` 的进度值 |
| T-PRG-02 | 进度条格式 | 格式：`[####-----] 40% \|`，长度固定，末尾为旋转动画字符（`-`/`\`/`\|`/`/`） |
| T-PRG-03 | 动画刷新率 | 每秒更新 8 次（125ms 定时器） |
| T-PRG-04 | 增量重绘 | 仅回退并重写变化的文本部分，避免闪烁 |
| T-PRG-05 | 重定向禁用 | 检测到输出被重定向时，不输出任何内容 |
| T-PRG-06 | 资源释放 | 实现 `IDisposable`，释放时清除进度条行 |

---

### 2.3 重试机制（Utils.RetryMethod）

#### 功能描述

为可能失败的操作提供通用的重试封装，支持同步/异步、有无返回值的四种场景。

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| T-RTY-01 | 同步有返回值重试 | `RetryMethod<T>(Func<T>, maxRetryCount, sleepTime, throwOnFailure, onRetry)` |
| T-RTY-02 | 同步无返回值重试 | `RetryMethod(Action, maxRetryCount, sleepTime, onRetry)` |
| T-RTY-03 | 异步有返回值重试 | `RetryMethodAsync<T>(Func<Task<T>>, ...)` |
| T-RTY-04 | 异步无返回值重试 | `RetryMethodAsync(Func<Task>, ...)` |
| T-RTY-05 | 默认参数 | `maxRetryCount = 3`，`sleepTime = 100ms`，`throwOnFailure = false` |
| T-RTY-06 | 重试回调 | `onRetry(当前次数, Exception)` 在每次失败后调用 |
| T-RTY-07 | 失败策略 | `throwOnFailure = false` 时捕获最后一次异常并通过 ConsoleEx 输出；`true` 时重新抛出 |
| T-RTY-08 | 参数验证 | `maxRetryCount < 0` 时抛出 `ArgumentException` |

---

### 2.4 Google Drive 下载器（GoogleFileDownloader）

#### 功能描述

无需 Google API Key，直接通过 HTTP 下载 Google Drive 文件，自动处理大文件下载确认页面。

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| T-GDL-01 | URL 格式兼容 | 自动将 `/open?id=`、`/file/d/`、`/uc?id=` 等格式统一转换为下载 URL |
| T-GDL-02 | 大文件确认处理 | 当 Google 返回病毒扫描确认页时，自动解析隐藏的 `confirm` 参数并重发下载请求（最多 3 次） |
| T-GDL-03 | Cookie 持久化 | 使用内部 `CookieAwareWebClient`，保持 Cookie 状态跨请求传递 |
| T-GDL-04 | 文件大小获取 | 解析 `Content-Range` 响应头获取实际文件总大小 |
| T-GDL-05 | 同步下载 | `DownloadFile(address, fileName)` |
| T-GDL-06 | 异步下载 | `DownloadFileAsync(address, fileName, userToken)` |
| T-GDL-07 | 进度事件 | 触发 `DownloadProgressChanged(BytesReceived, TotalBytesToReceive, ProgressPercentage)` |
| T-GDL-08 | 完成事件 | 触发 `DownloadFileCompleted` |

---

### 2.5 AI 客户端（AIClient）

#### 功能描述

轻量级 OpenAI 兼容 API 客户端，支持 OpenAI 和本地 Llama 服务，内置会话缓存。

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| T-AI-01 | 全局参数配置 | `SetClientParams(Action<AISetting>)` 配置 `ApiDomain`、`ApiKey`、`DefaultModel` |
| T-AI-02 | 客户端工厂（缓存） | `CreateAiChat(name)` 根据名称从 `DictionaryCache` 返回已有或新建客户端 |
| T-AI-03 | 发送对话请求 | `CreateChatCompletionAsync(CompletionRequest)` → `CompletionResponse` |
| T-AI-04 | 标准请求构建 | `CreateNormalRequest(Action<CompletionRequest>)` 预设 `Temperature=0.5`、`FrequencyPenalty=0` |
| T-AI-05 | 请求字段 | `model`、`messages[]`、`temperature`（0-1）、`max_tokens`（默认 16）、`top_p`、`presence_penalty`、`frequency_penalty`、`stream`、`stop[]` |
| T-AI-06 | 响应解析 | 解析 `choices[].message`、`finish_reason`、`usage.total_tokens` |

---

### 2.6 字符串扩展（StringHelper）

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| T-STR-01 | 忽略大小写相等判断 | `EqualIgnoreCase(params String[])` 任意一个匹配即返回 `true` |
| T-STR-02 | 忽略大小写前后缀 | `StartsWithIgnoreCase` / `EndsWithIgnoreCase` |
| T-STR-03 | 空值检查 | `IsNullOrEmpty` / `IsNullOrWhiteSpace` |
| T-STR-04 | 多分隔符分割 | `Split(params String[] separators)` 默认分隔符为 `","` 和 `";"` |
| T-STR-05 | 分割转整数数组 | `SplitAsInt()` → `Int32[]`，无效值跳过 |
| T-STR-06 | 字节转换 | `GetBytes(Encoding?)` 默认 UTF-8 无 BOM |
| T-STR-07 | StringBuilder 分隔符 | `Separate(StringBuilder, String separator)` 非空时追加分隔符 |

---

### 2.7 泛型 TTL 缓存（DictionaryCache\<TKey, TValue\>）

#### 功能描述

线程安全的带过期时间的字典缓存，可替代简单的 `ConcurrentDictionary`。

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| T-CAC-01 | TTL 支持 | `Expire` 属性（秒），`0` 表示永不过期 |
| T-CAC-02 | 自动计算缓存 | `GetItem(key, factory)` 缓存未命中时调用 `factory` 计算并缓存结果 |
| T-CAC-03 | 多参数工厂重载 | 支持工厂函数携带 1~4 个额外参数（避免闭包捕获） |
| T-CAC-04 | 自动清理 | 定时器按 `ClearPeriod`（秒）清理已过期条目 |
| T-CAC-05 | 异步刷新模式 | `Asynchronous = true` 时，缓存过期后后台异步刷新，旧值继续可用 |
| T-CAC-06 | 延迟加锁 | `DelayLock = true` 时先计算再加锁写入，提高并发吞吐 |
| T-CAC-07 | 默认值缓存 | `CacheDefault = true` 时缓存工厂返回的 `default(TValue)` |
| T-CAC-08 | 自动释放 | `AutoDispose = true` 时，过期条目实现 `IDisposable` 时自动调用 `Dispose()` |

---

### 2.8 性能分析工具（SimplePerformanceProfiler）

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| T-PRF-01 | 会话创建 | `StartNew(name)` 创建带名称的分析会话，内部启动 `Stopwatch` |
| T-PRF-02 | 步骤记录 | `Step(stepName)` 返回 `IDisposable` 的 `PerformanceStep`，`Dispose` 时记录耗时 |
| T-PRF-03 | 树形报告 | `GetReport()` 输出带层级缩进的耗时报告，含总计 |

---

## 3. 模块二：Octopus.Segment — 中文分词

> 目标框架：netstandard2.1  
> 依赖：嵌入式词典资源（dict.txt、HMM 矩阵、IDF、停用词）

### 3.1 核心分词器（JiebaSegmenter）

#### 功能描述

基于结巴算法的中文分词引擎，融合动态规划（DAG）与隐马尔可夫模型（HMM）。

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| S-SEG-01 | 精确模式分词 | `Cut(text, cutAll=false, hmm=true)` → `IEnumerable<String>` |
| S-SEG-02 | 全分词模式 | `cutAll=true` 时遍历所有词典词，速度快，召回高、精度低 |
| S-SEG-03 | HMM 未登录词识别 | `hmm=true` 时对不在词典中的字串用 Viterbi 算法解码，识别新词 |
| S-SEG-04 | 带位置信息分词 | `Cut2(text)` → `IEnumerable<WordInfo>`，含起止位置与词频 |
| S-SEG-05 | 搜索引擎模式 | `CutForSearch(text, hmm=true)` 对较长词追加 2-gram / 3-gram 分词，提升召回 |
| S-SEG-06 | Token 化 | `Tokenize(text, mode, hmm)` → `IEnumerable<Token>`，含 `word`、`start`、`end` |
| S-SEG-07 | 用户词典（文件） | `LoadUserDict(filePath)` 每行格式 `词 [频率] [词性]` |
| S-SEG-08 | 用户词典（嵌入资源） | `LoadUserDictForEmbedded(Assembly, resourcePath)` |
| S-SEG-09 | 用户词典（文本） | `LoadUserDictFromText(text)` |
| S-SEG-10 | 动态添加词 | `AddWord(word, freq, tag)` 实时更新 Trie 词典和前缀索引 |
| S-SEG-11 | 动态删除词 | `DeleteWord(word)` 将词频设为 0，等效删除 |

#### 算法约束

- DAG 构建使用 Trie 前向最大匹配
- 动态规划路径选择使用对数概率（`log(freq/total)`），避免浮点下溢
- HMM 状态序列：B（词首）/ M（词中）/ E（词尾）/ S（单字词）

---

### 3.2 词典管理（WordDictionary）

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| S-DIC-01 | 单例模式 | 全局共享一个词典实例（`Lazy<T>` 懒加载） |
| S-DIC-02 | 词频查询 | `GetFreqOrDefault(word)` 不存在时返回 `1` |
| S-DIC-03 | 词存在检查 | `ContainsWord(word)` 频率 > 0 视为存在 |
| S-DIC-04 | 建议频率计算 | `SuggestFreq(word, segments)` 根据分词结果的子词频率乘积推算词频 |
| S-DIC-05 | 前缀索引维护 | 添加词时同步写入所有前缀（`a`、`ab`、`abc`...），支持前向匹配 |

---

### 3.3 词性标注（PosSegmenter）

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| S-POS-01 | 词性标注分词 | `Cut(text, hmm=true)` → `IEnumerable<Pair<String>>` 含词与词性标签 |
| S-POS-02 | HMM 词性推断 | 对未登录词用 PoS 专属 Viterbi 解码推断词性 |

---

### 3.4 关键词提取

#### TF-IDF 提取（TfidfExtractor）

| 编号 | 需求 | 说明 |
|------|------|------|
| S-KW-01 | 关键词提取 | `ExtractTags(text, topK=20, allowPos)` → `IEnumerable<String>` |
| S-KW-02 | 带权重提取 | `ExtractTagsWithWeight(text, topK, allowPos)` → `IEnumerable<WordWeightPair>` |
| S-KW-03 | TF-IDF 计算 | `TF-IDF = (词频 / 文档长度) × log(总文档数 / 含该词文档数)` |
| S-KW-04 | 自定义 IDF | `SetIdfPath(filePath)` 加载自定义 IDF 词库 |
| S-KW-05 | 自定义停用词 | `SetStopWords(filePath)` 覆盖默认停用词表 |

#### TextRank 提取（TextRankExtractor）

| 编号 | 需求 | 说明 |
|------|------|------|
| S-KW-06 | 图算法提取 | 构建词共现有向图，迭代计算顶点权重（类 PageRank） |
| S-KW-07 | 可配置窗口 | 共现窗口大小可配置（影响边的构建） |
| S-KW-08 | 接口一致 | 与 TfidfExtractor 保持相同 `ExtractTags` / `ExtractTagsWithWeight` 接口 |

---

## 4. 模块三：Octopus.SearchCore — 全文搜索引擎

> 目标框架：net8.0  
> 依赖：Lucene.NET 4.8.0-beta、Octopus.Segment

### 4.1 搜索引擎门面（ISearchEngine）

#### 功能描述

搜索引擎主入口，统一管理索引写入与查询。

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| SC-SE-01 | 全量创建索引 | `CreateIndex()` 清空后重建所有实体索引 |
| SC-SE-02 | 指定表创建索引 | `CreateIndex(List<String> tables)` 按表名选择性重建 |
| SC-SE-03 | 删除全部索引 | `DeleteIndex()` 清空磁盘索引目录 |
| SC-SE-04 | 保存并同步索引 | `SaveChanges(index=true)` 持久化数据库并刷新索引 |
| SC-SE-05 | 异步保存 | `SaveChangesAsync(index=true)` |
| SC-SE-06 | 泛型搜索 | `Search<T>(SearchOptions)` → `ISearchResultCollection<T>`（不含相关性分数） |
| SC-SE-07 | 评分搜索 | `ScoredSearch<T>(SearchOptions)` → `IScoredSearchResultCollection<T>`（含分数） |
| SC-SE-08 | 单条最佳匹配 | `SearchOne<T>(SearchOptions)` → `T`（返回评分最高的一条） |
| SC-SE-09 | 自定义词库导入 | `ImportCustomerKeywords(IEnumerable<String>)` 热加载用户词，无需重启 |
| SC-SE-10 | 索引总数 | `IndexCount` 属性返回当前文档总数 |

---

### 4.2 索引写入器（ILuceneIndexer）

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| SC-IDX-01 | 添加文档 | `Add(ILuceneIndexable entity)` 调用 `entity.ToDocument()` 写入 Lucene |
| SC-IDX-02 | 批量创建索引 | `CreateIndex(entities, recreate=true)` |
| SC-IDX-03 | 删除文档 | `Delete(ILuceneIndexable entity)` 按 `IndexId` 删除 |
| SC-IDX-04 | 批量删除 | `Delete<T>(IList<T>)` |
| SC-IDX-05 | 清空索引 | `DeleteAll(commit=true)` |
| SC-IDX-06 | 更新文档 | `Update(ILuceneIndexable)` 先删后插 |
| SC-IDX-07 | 变更集更新 | `Update(LuceneIndexChangeset)` 批量处理 Added/Updated/Removed 三种状态 |
| SC-IDX-08 | 文档计数 | `Count()` → `Int32` |

---

### 4.3 索引查询器（ILuceneIndexSearcher）

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| SC-SRC-01 | 分词查询关键词 | `CutKeywords(keyword)` 使用结巴分词拆分，结果缓存 1 小时 |
| SC-SRC-02 | 精确短语搜索 | 关键词中 `"..."` 格式解析为 `Occur.MUST` 精确短语 |
| SC-SRC-03 | 排除词搜索 | `-词` 前缀解析为 `Occur.MUST_NOT` |
| SC-SRC-04 | 模糊搜索 | 普通关键词附加 `~` 后缀，允许编辑距离 1 的模糊匹配 |
| SC-SRC-05 | 单字段搜索 | `fields` 为单个时使用 `QueryParser` |
| SC-SRC-06 | 多字段搜索 | `fields` 为多个时使用 `MultiFieldQueryParser` + `Boosts` 权重 |
| SC-SRC-07 | 分页 | `SearchOptions.Skip` / `Take` 控制 |
| SC-SRC-08 | 排序 | `SearchOptions.OrderBy`（`List<SortField>`），默认按评分降序 |
| SC-SRC-09 | 评分阈值过滤 | `SearchOptions.Score`（默认 0.5f），低于阈值的结果不返回 |
| SC-SRC-10 | 安全搜索降级 | `ParseException` 时自动 `safeSearch=true`，去掉特殊语法重试 |
| SC-SRC-11 | 返回统计信息 | 结果集携带 `TotalHits`（总命中数）和 `Elapsed`（查询耗时） |

---

### 4.4 可索引实体接口（ILuceneIndexable）

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| SC-ENT-01 | 主键属性 | 实体必须有 `Id` 属性（`Int32`/`Int64`/`String`/`Guid` 均支持） |
| SC-ENT-02 | 索引标识 | `IndexId` 属性（`String`），用于增量更新时定位文档 |
| SC-ENT-03 | 文档转换 | `ToDocument()` 方法：反射遍历 `[LuceneIndex]` 标注属性，按类型创建对应 Lucene 字段 |

---

### 4.5 LuceneIndexAttribute 特性

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| SC-ATT-01 | 字段名 | `Name` 属性，默认使用 C# 属性名 |
| SC-ATT-02 | 是否存储原值 | `Store = Field.Store.YES` 时可从 Document 直接取原始值 |
| SC-ATT-03 | 标注位置 | 可标注在实体属性上，类级别不适用 |

---

### 4.6 搜索配置（SearchOptions）

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| SC-OPT-01 | 关键词（必填） | `Keywords` 非空，否则抛出异常 |
| SC-OPT-02 | 字段过滤 | `Fields: List<String>` 为空时搜索所有字段 |
| SC-OPT-03 | 字段权重 | `Boosts: Dict<String, Single>` 默认权重 2.0f |
| SC-OPT-04 | 最大命中数 | `MaximumNumberOfHits`，默认 1000 |
| SC-OPT-05 | 分页构造 | `SearchOptions(keywords, page, size, fields)` 自动计算 Skip/Take |
| SC-OPT-06 | 类型自动字段 | `SearchOptions(keywords, page, size, Type t)` 通过反射提取该类的 `[LuceneIndex]` 字段列表 |

---

### 4.7 标签提取工具（TagUtils）

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| SC-TAG-01 | 预置标签库 | 内置 `tag_role.txt`（角色标签）和 `tag_scene.txt`（场景标签）作为嵌入资源 |
| SC-TAG-02 | 自定义词库分词 | `GetSegmenter()` 将标签库词条加载为用户词典 |
| SC-TAG-03 | 全量标签获取 | `GetAllTags()` → `List<String>` |
| SC-TAG-04 | JSON 输出 | `GetAllTagsToJson()` → `String` |

---

## 5. 模块四：OctopusEx.WebCore — Web 脚手架

> 目标框架：net10.0  
> 架构风格：DDD + CQRS + Repository Pattern

### 5.1 自动依赖注入（Dependency）

#### 功能描述

通过标记接口 + 程序集扫描，替代手动 `services.AddXxx()` 注册，降低遗漏风险。

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| W-DI-01 | 生命周期标记接口 | `ISingletonDependency`（单例）、`IScopeDependency`（作用域）、`ITransientDependency`（瞬态） |
| W-DI-02 | 自动扫描注册 | `DependencyServiceRegistrar` 扫描所有程序集，找到实现标记接口的类并注册 |
| W-DI-03 | 接口优先注册 | 如服务类有非标记接口，按接口注册；否则按自身类型注册 |
| W-DI-04 | 优先级控制 | `[IocAttribute(Priority = n)]` 控制多实现时选择哪个（值越大优先级越高） |
| W-DI-05 | 防重复注册 | 使用 `TryAdd()` 避免覆盖已有注册 |
| W-DI-06 | 最佳实践 | **服务接口**继承生命周期接口，**实现类**不直接继承，避免接口污染 |

#### 示例（推荐方式）

```csharp
// 服务接口继承生命周期接口
public interface IOrderService : IScopeDependency { ... }

// 实现类只继承服务接口
public class OrderService : IOrderService { ... }
```

---

### 5.2 仓储与工作单元模式（DomainCore）

#### IQuery\<TEntity, TKey\>（查询接口）

| 编号 | 需求 | 说明 |
|------|------|------|
| W-QRY-01 | 基础查询 | `Find()` → `IQueryable<TEntity>` 供上层组合 |
| W-QRY-02 | 条件查询 | `Find(Expression<Func<T,bool>>)` |
| W-QRY-03 | 主键查询 | `GetByIdAsync(TKey)` → `TEntity?` |
| W-QRY-04 | 批量主键查询 | `GetByIdsAsync(IEnumerable<TKey>)` → `List<TEntity>` |
| W-QRY-05 | 全量查询 | `GetAllAsync()` → `List<TEntity>` |
| W-QRY-06 | 条件列表查询 | `FindAllAsync(condition, orderBy?, includes?)` |
| W-QRY-07 | 构建器查询 | `FindAllAsync(IQueryBuilder)` 支持链式构建 |
| W-QRY-08 | 单条查询 | `SingleAsync(condition)` 多于 1 条抛异常 |
| W-QRY-09 | 存在性检查 | `ExistsAsync(condition)` → `Boolean` |
| W-QRY-10 | 统计数量 | `CountAsync(condition)` → `Int64` |

#### ICommand\<TEntity, TKey\>（命令接口）

| 编号 | 需求 | 说明 |
|------|------|------|
| W-CMD-01 | 添加 | `AddAsync(TEntity)` |
| W-CMD-02 | 批量添加 | `AddRangeAsync(IEnumerable<TEntity>)` |
| W-CMD-03 | 更新 | `UpdateAsync(TEntity)` |
| W-CMD-04 | 批量更新 | `UpdateRangeAsync(IEnumerable<TEntity>)` |
| W-CMD-05 | 按实体删除 | `DeleteAsync(TEntity)` |
| W-CMD-06 | 按主键删除 | `DeleteByIdAsync(TKey)` |
| W-CMD-07 | 批量删除 | `DeleteRangeAsync(IEnumerable<TEntity>)` |

#### IUnitOfWork（工作单元）

| 编号 | 需求 | 说明 |
|------|------|------|
| W-UOW-01 | 获取仓储 | `GetRepository<TEntity, TKey>()` 懒加载，同一 UoW 内共享实例 |
| W-UOW-02 | 保存变更 | `SaveChangesAsync()` → 影响行数 |
| W-UOW-03 | 手动事务 | `BeginTransactionAsync()` / `CommitTransactionAsync()` / `RollbackTransactionAsync()` |
| W-UOW-04 | 包装事务 | `ExecuteTransactionAsync(Func<Task>)` 自动提交/回滚，异常时回滚 |

---

### 5.3 CRUD 服务基类（CrudServiceBase）

#### 功能描述

封装 CRUD 通用逻辑，子类只需实现映射和验证，不重复写增删改查代码。

#### 映射抽象（子类必须或可选重写）

| 编号 | 方法 | 说明 |
|------|------|------|
| W-SVC-01 | `MapToDto(TEntity)` | 实体 → DTO（虚方法，子类重写） |
| W-SVC-02 | `MapToEntity(TCreateDto)` | 创建 DTO → 新实体 |
| W-SVC-03 | `UpdateEntityFromDto(TEntity, TUpdateDto)` | 用更新 DTO 修改现有实体 |
| W-SVC-04 | `MapToDtoList(List<TEntity>)` | 批量映射 |
| W-SVC-05 | `GetUpdateRequestId(TUpdateDto)` | 抽象方法，从更新请求中提取主键（必须实现） |
| W-SVC-06 | `GetEntityId(TEntity)` | 抽象方法，从实体提取主键（必须实现） |

#### 生命周期钩子（虚方法，可选重写）

| 编号 | 钩子 | 触发时机 |
|------|------|---------|
| W-HK-01 | `BeforeCreateAsync(entity, request)` | 创建前，可用于设置默认值 |
| W-HK-02 | `AfterCreateAsync(entity, request)` | 创建后，可用于发送事件 |
| W-HK-03 | `BeforeUpdateAsync(entity, request)` | 更新前 |
| W-HK-04 | `AfterUpdateAsync(entity, request)` | 更新后 |
| W-HK-05 | `CanDeleteAsync(id)` | 删除前检查，返回 `DeleteCheckResult`（含 `CanDelete`、`Reason`） |
| W-HK-06 | `CanDeleteBatchAsync(ids)` | 批量删除前检查 |
| W-HK-07 | `ApplyListFilter(query, request)` | 应用业务过滤条件 |
| W-HK-08 | `ApplyListSorting(query, request)` | 应用排序规则 |

#### 验证（虚方法）

| 编号 | 方法 | 说明 |
|------|------|------|
| W-VAL-01 | `ValidateCreateRequest(request)` | 返回 `ValidationResult`，`IsValid=false` 时抛出业务异常 |
| W-VAL-02 | `ValidateUpdateRequest(request)` | 同上 |

---

### 5.4 CRUD 控制器基类（CURDControllerBase）

#### 功能需求

| 编号 | HTTP 端点 | 说明 |
|------|-----------|------|
| W-CTL-01 | `GET /{id}` | 获取单个实体 DTO |
| W-CTL-02 | `GET /` | 分页列表，支持 `PageRequest`（页码、每页大小、搜索词） |
| W-CTL-03 | `GET /all` | 获取全部记录（无分页） |
| W-CTL-04 | `POST /` | 创建，请求体为 `TCreateDto` |
| W-CTL-05 | `PUT /{id}` | 更新，请求体为 `TUpdateDto` |
| W-CTL-06 | `DELETE /{id}` | 删除单个 |
| W-CTL-07 | `POST /bulk-delete` | 批量删除，请求体为 `IEnumerable<TKey>` |
| W-CTL-08 | 统一响应格式 | 所有端点返回 `BaseResponse<T>` / `BaseResponsePaged<T>`，包含 `Success`、`Code`、`Message`、`Data` |
| W-CTL-09 | 异常处理 | 控制器层捕获业务异常并转换为对应 HTTP 状态码响应 |

---

### 5.5 审计日志（AuditInterceptor）

#### 功能描述

EF Core `SaveChanges` 拦截器，无侵入地记录所有实体变更（前后值、操作人、时间）。

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| W-AUD-01 | 自动拦截 | 注册后对所有 `SaveChanges[Async]` 生效，业务代码无感知 |
| W-AUD-02 | 三种操作记录 | `INSERT`（Added）、`UPDATE`（Modified）、`DELETE`（Deleted） |
| W-AUD-03 | 变更值记录 | `OldValues` 和 `NewValues` 序列化为 JSON，存入审计日志 |
| W-AUD-04 | 变更字段列表 | `ChangedProperties: List<String>` 只记录有变化的字段名 |
| W-AUD-05 | 操作人信息 | 通过 `IAuditConfiguration.GetCurrentUser()` 获取 `UserId`、`UserName`、`IpAddress`、`UserAgent` |
| W-AUD-06 | 时间戳 | `CreatedTime = DateTime.UtcNow` |
| W-AUD-07 | 跳过审计实体本身 | `AuditLog` 实体的变更不产生新审计记录，避免循环 |
| W-AUD-08 | 全局忽略字段 | `GlobalIgnoredProperties` 配置跨实体忽略字段（如 `RowVersion`） |
| W-AUD-09 | 领域级忽略字段 | `DomainAuditConfiguration.IgnoredProperties` 按领域配置 |
| W-AUD-10 | 注册扩展方法 | `AddAuditService(services, config)` 一行注册拦截器 |

---

### 5.6 Hangfire 后台任务（HangfireExtensions）

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| W-HF-01 | 内存存储配置 | `AddSimpleHangfire(workerCount=1)` 使用 `MemoryStorage`，重启后任务丢失 |
| W-HF-02 | 一次性作业 | `AddBackgroundJob(name, action)` 立即异步执行 |
| W-HF-03 | 延迟作业 | `AddDelayedJob(name, action, delay)` 延迟指定时长后执行 |
| W-HF-04 | 定时作业 | `AddRecurringJob(jobId, action, cronExpression)` Cron 表达式调度 |
| W-HF-05 | 移除循环任务 | `RemoveRecurringJob(jobId)` |
| W-HF-06 | 防重复执行 | 带 `WithLock` 后缀方法，通过静态字典实现单实例保护（生产环境建议改用 Redis） |
| W-HF-07 | Fallback 降级 | Hangfire 注册失败时自动降级为 `Task.Run()` 或 `Timer` 执行 |
| W-HF-08 | Dashboard 认证 | 从 `appsettings.json` 读取用户名密码配置 Dashboard 访问权限 |

---

### 5.7 健康检查（HealthCheckExtensions）

#### 功能需求

| 编号 | 端点 | 说明 |
|------|------|------|
| W-HC-01 | `GET /health/ready` | 就绪探针：检查标记为 `ready` 的检查项（数据库、缓存等） |
| W-HC-02 | `GET /health/live` | 存活探针：只检查标记为 `live` 的项，通常只检查进程本身 |
| W-HC-03 | `GET /health/full` | 完整检查：执行所有已注册检查项 |
| W-HC-04 | `GET /health` | 详细状态：返回每个检查的名称、状态、耗时、描述（JSON） |

| 编号 | 注册方法 | 说明 |
|------|---------|------|
| W-HC-05 | `AddDatabaseHealthCheck(connStr, dbType)` | 数据库连接性检查，连接字符串脱敏后写入描述 |
| W-HC-06 | `AddExternalApiHealthCheck(name, url, timeout)` | HTTP GET 探测外部 API，支持超时配置 |
| W-HC-07 | `AddCacheHealthCheck(name, factory)` | 缓存服务检查，支持命中率统计 |
| W-HC-08 | `AddBusinessLogicHealthCheck(name, func)` | 注入自定义 `ICustomHealthCheck` 实现 |

---

### 5.8 API 文档 UI（ApiUIExtensions）

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| W-UI-01 | Swagger UI | `AddSwaggerUIServices()` + `UseSwaggerUI()` 挂载 Swagger |
| W-UI-02 | Scalar UI | `UseScalarUI()` 挂载更现代的 Scalar 文档 UI |
| W-UI-03 | XML 注释 | 读取 XML 文档文件，展示方法 Summary 注释 |

---

### 5.9 链路追踪（AspireExtensions）

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| W-OTL-01 | OpenTelemetry 配置 | `AddDefaultOpenTelemetry(builder, serviceName)` 一行接入 OTEL |
| W-OTL-02 | 自动仪表化 | 包含 HTTP、EF Core、Hangfire 的 Trace 仪表化 |

---

### 5.10 敏感词过滤插件（SensitiveWordFilterPlugin）

#### 功能描述

三层敏感词检测：词典快速匹配 → AI 语义识别 → 综合判断。

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| W-SWF-01 | 快速检测 | `DetectSensitiveWords(text)` 使用 ToolGood.Words 词典匹配，毫秒级 |
| W-SWF-02 | AI 检测 | `DetectSensitiveWordsWithAI(text)` 调用 Semantic Kernel，识别语义敏感内容 |
| W-SWF-03 | 综合检测 | `ComprehensiveDetectSensitiveWords(text)` 先快速检测，再 AI 兜底 |
| W-SWF-04 | 词库管理 | `SetSensitiveWords(words)` 批量设置 / `AddSensitiveWord(word)` 单个添加 |
| W-SWF-05 | 词库查询 | `GetAllSensitiveWords()` → `List<String>` |
| W-SWF-06 | 内置词库 | 内置 65+ 条默认敏感词 |
| W-SWF-07 | AI 可选 | 构造函数中 `Kernel` 参数可空，不注入时降级为仅词典模式 |

---

### 5.11 条件查询扩展（EFQueryableExtensions.WhereIf）

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| W-WIF-01 | IQueryable 条件在后 | `WhereIf<T>(source, predicate, condition)` condition 为 true 时追加 where |
| W-WIF-02 | IQueryable 条件在前 | `WhereIf<T>(source, condition, predicate)` 参数顺序不同的重载 |
| W-WIF-03 | IQueryable 带索引 | `WhereIf<T>(source, Expression<Func<T,Int32,Boolean>>, condition)` |
| W-WIF-04 | IEnumerable 版本 | `WhereIf<T>(IEnumerable<T>, Func<T,Boolean>, condition)` |
| W-WIF-05 | IEnumerable 带索引 | `WhereIf<T>(IEnumerable<T>, Func<T,Int32,Boolean>, condition)` |

---

### 5.12 EF Core 枚举字符串映射（EnumStringAttribute）

#### 功能需求

| 编号 | 需求 | 说明 |
|------|------|------|
| W-ENM-01 | 枚举存储字符串 | `[EnumString]` 标注枚举属性，EF Core 模型配置时自动以字符串形式存储 |
| W-ENM-02 | 注册扩展 | `EnumStringExtension.UseEnumStringConvention(modelBuilder)` 自动扫描并应用 |

---

## 6. 跨模块集成需求

### 6.1 工具库 → 搜索引擎

| 编号 | 需求 | 说明 |
|------|------|------|
| I-01 | 日志输出 | SearchCore 内部使用 `ConsoleEx.Info/Error` 输出索引/搜索日志 |
| I-02 | 重试机制 | 索引构建操作可用 `Utils.RetryMethodAsync` 包装，处理 Lucene 偶发锁冲突 |

### 6.2 分词 → 搜索引擎

| 编号 | 需求 | 说明 |
|------|------|------|
| I-03 | 中文分析器 | `JieBaAnalyzer` 封装 `JiebaSegmenter`，作为 Lucene `Analyzer` 使用 |
| I-04 | 查询分词 | `LuceneIndexSearcher.CutKeywords()` 调用 `JiebaSegmenter.Cut()` |
| I-05 | 热词导入 | `SearchEngine.ImportCustomerKeywords()` 调用 `JiebaSegmenter.AddWord()` |

### 6.3 WebCore 数据流

```
HTTP 请求
  → CURDControllerBase（路由/响应格式）
  → CrudServiceBase（业务逻辑/验证/钩子）
  → IRepository（IQuery + ICommand）
  → IUnitOfWork.SaveChangesAsync()
  → AuditInterceptor（自动记录变更）
  → AuditLog 写入数据库
```

### 6.4 启动流程

```
Bootstrapper.Start()
  → AppDomainAssemblyFinder 扫描程序集
  → AppDomainTypeFinder 查找 IServiceRegistrar 实现
  → 按 OrderId 排序后执行 ConfigureServices
  → DependencyServiceRegistrar 扫描生命周期接口自动注册
  → 业务服务注册（数据库上下文、仓储、领域服务）
  → 应用就绪
```

---

## 7. 非功能性需求

### 7.1 兼容性

| 需求 | 说明 |
|------|------|
| Octopus.Tools 支持 .NET Standard 2.0 | 兼容 .NET Framework 4.6.1+、.NET Core 2.0+ |
| Octopus.Segment 支持 .NET Standard 2.1 | 兼容 .NET Core 3.0+、.NET 5+ |
| Octopus.SearchCore 支持 net8.0 | 需 .NET 8 及以上 |
| OctopusEx.WebCore 支持 net10.0 | 需 .NET 10 及以上 |

### 7.2 性能

| 需求 | 说明 |
|------|------|
| 控制台写入非阻塞 | 主线程投入队列后立即返回，不等待 I/O 完成 |
| 分词结果缓存 | 搜索查询的分词结果缓存 1 小时，避免重复分词 |
| 仓储懒加载 | `CrudServiceBase.Repository` 延迟加载，不使用时不初始化 |
| DictionaryCache 并发安全 | 使用 ConcurrentDictionary + 延迟加锁，支持高并发读场景 |

### 7.3 安全

| 需求 | 说明 |
|------|------|
| Hangfire Dashboard 认证 | 必须配置用户名密码，不可匿名访问 |
| 数据库连接字符串脱敏 | 健康检查描述中不暴露完整连接字符串 |
| 敏感词过滤三级兜底 | 词典 + AI 双重检测，降低漏检风险 |

### 7.4 可观测性

| 需求 | 说明 |
|------|------|
| 健康检查 4 端点 | 支持 K8s readiness/liveness probe 标准接口 |
| OpenTelemetry 支持 | 可接入 Jaeger、Tempo 等分布式追踪系统 |
| 审计日志 | 所有数据变更可追溯操作人、时间、变更内容 |

### 7.5 代码规范

| 需求 | 说明 |
|------|------|
| 完整类型名 | 使用 `String`/`Int32`/`Boolean` 等 .NET 完整类型名，禁用 C# 别名 |
| Conventional Commits | 提交信息遵循约定式提交规范 |
| Husky 自动格式化 | 提交前自动运行 `dotnet format`，保证代码风格一致 |
| 强命名签名 | 所有程序集用 `octopus-key.snk` 签名，不可删除强命名配置 |
| XML 文档注释 | 公开 API 需有 XML 注释，支持 NuGet 包的 IntelliSense |

### 7.6 打包与发布

| 需求 | 说明 |
|------|------|
| NuGet 打包 | 各项目配置 `<GeneratePackageOnBuild>`，产出 `.nupkg` |
| 符号包 | 同步生成 `.snupkg`，支持源码调试 |
| 图标 | NuGet 包图标使用 `favicon.png` |
| 仓库元数据 | 包含 `<RepositoryUrl>`、`<License>`、`<Authors>` 等标准元数据 |

---

*本文档由代码反向推导生成，如有业务需求与代码实现不一致之处，以代码为准。*
