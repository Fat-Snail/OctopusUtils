# 单元测试工具类使用指南

## 概述

Octopus.Utils 提供了一个轻量级的单元测试框架，专为 .NET 项目设计。包含性能分析工具、断言工具和测试报告格式化功能。

## 快速开始

### 基本测试结构

```csharp
using Octopus.Utils.UnitTest;

[TestClass]
public class MyTests
{
    [TestMethod]
    public void BasicTest()
    {
        // Arrange
        var expected = 42;
        var actual = Calculate();
        
        // Assert
        Assert.AreEqual(expected, actual);
    }
}
```

### 性能分析测试

```csharp
[TestMethod]
public void PerformanceTest()
{
    using var profiler = MiniProfiler.StartNew("性能测试");

    using (var step = profiler.Step("步骤1"))
    {
        // 执行一些操作
        System.Threading.Thread.Sleep(10);
    }

    using (var step = profiler.Step("步骤2"))
    {
        // 执行其他操作
        System.Threading.Thread.Sleep(20);
    }

    var report = profiler.GetReport();
    var reportText = report.ToString();

    Console.WriteLine(reportText);
    
    // 验证性能报告
    Assert.IsTrue(reportText.Contains("性能分析报告"));
    Assert.IsTrue(reportText.Contains("总耗时"));
}
```

### 复杂性能测试示例

```csharp
[TestMethod]
public void ProfilerReportFormatTest()
{
    using var profiler = MiniProfiler.StartNew("FormatTest");

    using (var step = profiler.Step("TestStep"))
    {
        System.Threading.Thread.Sleep(10);
    }

    var report = profiler.GetReport();
    var reportText = report.ToString();

    Console.WriteLine(reportText);

    // 验证报告格式
    Assert.IsTrue(reportText.Contains("性能分析报告"));
    Assert.IsTrue(reportText.Contains("总耗时"));
    Assert.IsTrue(reportText.Contains("调用层级"));
    Assert.IsTrue(reportText.Contains("FormatTest"));
    Assert.IsTrue(reportText.Contains("TestStep"));
}
```

## 主要组件

### 1. TestClass 特性

用于标记测试类：

```csharp
[TestClass]
public class MyTestClass
{
    // 测试方法
}
```

### 2. TestMethod 特性

用于标记测试方法：

```csharp
[TestMethod]
public void MyTestMethod()
{
    // 测试逻辑
}
```

### 3. Assert 断言类

提供丰富的断言方法：

```csharp
// 基本断言
Assert.AreEqual(expected, actual);
Assert.AreNotEqual(expected, actual);
Assert.IsTrue(condition);
Assert.IsFalse(condition);
Assert.IsNull(obj);
Assert.IsNotNull(obj);

// 数值断言
Assert.AreEqual(expected, actual, "数值应该相等");
Assert.AreEqual(expected, actual, 0.001, "浮点数比较");

// 字符串断言
Assert.Contains(text, substring);
Assert.StartsWith(text, prefix);
Assert.EndsWith(text, suffix);

// 集合断言
Assert.AreEqual(expectedCount, collection.Count);
Assert.Contains(collection, item);
```

### 4. MiniProfiler 性能分析

用于性能测试和分析：

```csharp
using var profiler = MiniProfiler.StartNew("测试名称");

// 添加步骤
using (var step = profiler.Step("步骤名称"))
{
    // 执行代码
}

// 获取报告
var report = profiler.GetReport();
Console.WriteLine(report.ToString());
```

## 性能报告格式

性能分析报告包含以下信息：

- **性能分析报告** - 报告标题
- **总耗时** - 整体执行时间
- **调用层级** - 函数调用层次结构
- **各步骤耗时** - 每个步骤的详细耗时
- **内存使用** - 内存分配情况

## 高级用法

### 嵌套性能测试

```csharp
[TestMethod]
public void NestedPerformanceTest()
{
    using var profiler = MiniProfiler.StartNew("嵌套测试");

    using (var outerStep = profiler.Step("外部步骤"))
    {
        using (var innerStep = profiler.Step("内部步骤1"))
        {
            // 内部操作1
        }
        
        using (var innerStep2 = profiler.Step("内部步骤2"))
        {
            // 内部操作2
        }
    }
}
```

### 条件测试

```csharp
[TestMethod]
public void ConditionalTest()
{
    var condition = SomeCondition();
    
    if (condition)
    {
        Assert.IsTrue(true, "条件满足时应该执行");
    }
    else
    {
        Assert.Inconclusive("条件不满足，跳过测试");
    }
}
```

### 异常测试

```csharp
[TestMethod]
public void ExceptionTest()
{
    // 测试是否抛出预期异常
    Assert.ThrowsException<InvalidOperationException>(() => 
    {
        throw new InvalidOperationException("测试异常");
    });
}
```

## 最佳实践

### 1. 测试命名规范

```csharp
// 推荐的测试命名
[TestMethod]
public void Method_Name_Should_ExpectedResult_When_Condition()
{
    // 测试实现
}

// 示例
[TestMethod]
public void Add_Should_Return_Correct_Sum_When_Two_Positive_Numbers()
{
    var result = Add(2, 3);
    Assert.AreEqual(5, result);
}
```

### 2. Arrange-Act-Assert 模式

```csharp
[TestMethod]
public void CalculateTotal_Test()
{
    // Arrange - 准备测试数据
    var price = 100;
    var quantity = 5;
    var expected = 500;
    
    // Act - 执行被测试的方法
    var actual = CalculateTotal(price, quantity);
    
    // Assert - 验证结果
    Assert.AreEqual(expected, actual);
}
```

### 3. 性能测试建议

```csharp
[TestMethod]
public void PerformanceBestPractice()
{
    // 预热
    for (int i = 0; i < 100; i++)
    {
        MethodUnderTest();
    }
    
    // 正式测试
    using var profiler = MiniProfiler.StartNew("MethodUnderTest_Performance");
    
    for (int i = 0; i < 1000; i++)
    {
        MethodUnderTest();
    }
    
    var report = profiler.GetReport();
    
    // 验证性能基准
    Assert.IsTrue(report.TotalMilliseconds < 100, "方法应该在100ms内完成");
}
```

## 常见问题

### Q: 如何运行测试？
A: 使用 `dotnet test` 命令或 Visual Studio 的测试资源管理器。

### Q: 如何生成测试报告？
A: 测试完成后会自动生成报告，可以通过 Console.WriteLine 输出或保存到文件。

### Q: 性能分析的精度如何？
A: MiniProfiler 使用高精度计时器，精度达到毫秒级。

### Q: 是否支持异步测试？
A: 支持，使用 `async Task` 返回类型即可。

## 示例项目

完整的示例项目请参考 `Octopus.Utils.Test` 项目，包含各种测试场景的实现。

---

> 📚 更多详细文档请查看 [CHANGELOG.md](CHANGELOG.md) 了解版本更新信息。