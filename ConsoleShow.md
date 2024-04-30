# 谷歌云盘下载工具类

示例：

```C#

using (var consoleProgressBar = new Octopus.Tools.ConsoleProgressBar())
{
    for (int i = 1; i <= 100; i++)
    {
        //业务逻辑
        
        //coding
        
        //业务逻辑
        
        var rate=Math.Round((i / (double)100), 2);//取两位小数
        
        consoleProgressBar.Report(rate);//对线程安全
        
        Thread.Sleep(1000);//模拟一下卡顿
    }
}

```