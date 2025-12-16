using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Util.Helpers;
using Util.Infrastructure;
using Util.Reflections;

namespace OctopusEx.WebCore;

/// <summary>
/// 启动器
/// </summary>
public class Bootstrapper
{
    private const String ASSEMBLY_SKIP_PATTERN = "^System|^Mscorlib|^msvcr120|^Netstandard|^Microsoft|^Autofac|^AutoMapper|^EntityFramework|^Newtonsoft|^Castle|^NLog|^Pomelo|^AspectCore|^Xunit|^Nito|^Npgsql|^Exceptionless|^MySqlConnector|^Anonymously Hosted|^libuv|^api-ms|^clrcompression|^clretwrc|^clrjit|^coreclr|^dbgshim|^e_sqlite3|^hostfxr|^hostpolicy|^MessagePack|^mscordaccore|^mscordbi|^mscorrc|sni|sos|SOS.NETCore|^sos_amd64|^SQLitePCLRaw|^StackExchange|^Swashbuckle|WindowsBase|ucrtbase|^DotNetCore.CAP|^MongoDB|^Confluent.Kafka|^librdkafka|^EasyCaching|^RabbitMQ|^Consul|^Dapper|^EnyimMemcachedCore|^Pipelines|^DnsClient|^IdentityModel|^zlib|^NSwag|^Humanizer|^NJsonSchema|^Namotion|^ReSharper|^JetBrains|^NuGet|^ProxyGenerator|^testhost|^MediatR|^Polly|^AspNetCore|^Minio|^SixLabors|^Quartz|^Hangfire|^Handlebars|^Serilog|^WebApiClientCore|^BouncyCastle|^RSAExtensions|^MartinCostello|^Dapr.|^Bogus|^Azure.|^Grpc.|^HealthChecks|^Google|^CommunityToolkit|^Elasticsearch|^ICSharpCode|Enums.NET|^IdentityServer4|JWT|^MathNet|^MK.Hangfire|Mono.TextTemplating|Nest|^NPOI|^Oracle|Spire.Pdf|^FileSignatures";
    /// <summary>
    /// 主机生成器
    /// </summary>
    private readonly IHostBuilder _hostBuilder;
    /// <summary>
    /// 程序集查找器
    /// </summary>
    private readonly IAssemblyFinder _assemblyFinder;
    /// <summary>
    /// 类型查找器
    /// </summary>
    private readonly ITypeFinder _typeFinder;
    /// <summary>
    /// 服务配置操作列表
    /// </summary>
    private readonly List<Action> _serviceActions;

    /// <summary>
    /// 初始化启动器
    /// </summary>
    /// <param name="hostBuilder">主机生成器</param>
    public Bootstrapper( IHostBuilder hostBuilder ) {
        _hostBuilder = hostBuilder ?? throw new ArgumentNullException( nameof( hostBuilder ) );
        _assemblyFinder = new AppDomainAssemblyFinder { AssemblySkipPattern = ASSEMBLY_SKIP_PATTERN };
        _typeFinder = new AppDomainTypeFinder( _assemblyFinder );
        _serviceActions = new List<Action>();
    }

    /// <summary>
    /// 启动
    /// </summary>
    public virtual void Start() {
        ConfigureServices();
        ResolveServiceRegistrar();
        //ExecuteServiceActions();
    }

    /// <summary>
    /// 配置服务
    /// </summary>
    protected virtual void ConfigureServices() {
        _hostBuilder.ConfigureServices( ( context, services ) => {
            Util.Helpers.Config.SetConfiguration( context.Configuration );
            services.TryAddSingleton( _assemblyFinder );
            services.TryAddSingleton( _typeFinder );
        } );
    }

    /// <summary>
    /// 解析服务注册器
    /// </summary>
    protected virtual void ResolveServiceRegistrar() {
        var types = _typeFinder.Find<IServiceRegistrar>();
        var instances = types.Select( type => Reflection.CreateInstance<IServiceRegistrar>( type ) ).Where( t => t.Enabled ).OrderBy( t => t.OrderId ).ToList();
        var context = new ServiceContext( _hostBuilder, _assemblyFinder, _typeFinder );
        instances.ForEach( t => _serviceActions.Add( t.Register( context ) ) );
    }

    /// <summary>
    /// 执行延迟服务注册操作
    /// </summary>
    protected virtual void ExecuteServiceActions() {
        _serviceActions.ForEach( action => action?.Invoke() );
    }
}