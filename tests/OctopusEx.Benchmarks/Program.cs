using BenchmarkDotNet.Running;

// 用法：
//   dotnet run --project tests/OctopusEx.Benchmarks -c Release -- --filter '*'
// 单独跑一个：
//   dotnet run --project tests/OctopusEx.Benchmarks -c Release -- --filter '*Cache*'
BenchmarkSwitcher.FromAssembly(typeof(BenchmarksAssemblyMarker).Assembly).Run(args);

internal sealed class BenchmarksAssemblyMarker { }
