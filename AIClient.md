# 简易的AI客户端
实现C#代码与AI大模型交互，如：OpenAi、LlamaV2、LlamaV3

## 友情感谢
[@Soar360](https://github.com/Soar360)

[DK](jqk.feinian.net)

## 示例代码
1、配置客户端参数
```C#

AIClient.SetClientParams(s =>
{
    s.ApiDomain = "https://api.jqk.ai";//(可选)默认是 https://api.openai.com
    s.ApiKey = "sk-xxx";//(可选)本地模型无密码可置空
    s.DefaultModel = "gpt-3.5-turbo";//(必要)对话模型名称
});

```
2、实例化客户端
```C#
var client = AIClient.CreateAiChat("User1");//User1为任意名称
```

3、创建请求
```C#
var request = new CompletionRequest()
{
    FrequencyPenalty = 0,
    Model = "gpt-3.5-turbo",
    Temperature = 0.5,
    PresencePenalty = 0,
    Stream = false,
    Messages = new List<CompletionMessage>()
    {
        new CompletionMessage { Role = "user",Content = "Hello,Just Test"}
    }
};
```

或者
```C#
var request = client.CreateNormalRequest(req =>
{
    req.Messages.Add(new CompletionMessage(){ Role = "user",Content = "Hello,Just Test" });
});
```

4、提交请求并返回结果

```C#
var chatResponse = await client.CreateChatCompletionAsync(request);

if (chatResponse != null)
{
    Console.WriteLine(chatResponse.Choices.FirstOrDefault().Message.Content);
}
```



## 未实现的功能
- 更多查询Api接口
- 支持Stream流
- 支持文件上传


Enjoy～  ^_^





## 新生命开发团队
![XCode](https://newlifex.com/logo.png)

新生命团队（NewLife）成立于2002年，是新时代物联网行业解决方案提供者，致力于提供软硬件应用方案咨询、系统架构规划与开发服务。  
团队主导的70多个开源项目已被广泛应用于各行业，Nuget累计下载量高达100余万次。  
团队开发的大数据中间件NewLife.XCode、蚂蚁调度计算平台AntJob、星尘分布式平台Stardust、缓存队列组件NewLife.Redis以及物联网平台FIoT，均成功应用于电力、高校、互联网、电信、交通、物流、工控、医疗、文博等行业，为客户提供了大量先进、可靠、安全、高质量、易扩展的产品和系统集成服务。

我们将不断通过服务的持续改进，成为客户长期信赖的合作伙伴，通过不断的创新和发展，成为国内优秀的IoT服务供应商。

`新生命团队始于2002年，部分开源项目具有20年以上漫长历史，源码库保留有2010年以来所有修改记录`  
网站：https://newlifex.com  
开源：https://github.com/newlifex  
QQ群：1600800/1600838 


