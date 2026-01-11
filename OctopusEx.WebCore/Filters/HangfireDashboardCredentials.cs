using System;
using Microsoft.Extensions.Configuration;

namespace OctopusEx.WebCore.Filters
{
    // 集中管理 Hangfire Dashboard 的认证凭据，来自配置源（环境变量/appsettings.json）
    public static class HangfireDashboardCredentials
    {
        public static string Username { get; set; } = Environment.GetEnvironmentVariable("HANGFIRE_USERNAME") ?? "admin";
        public static string Password { get; set; } = Environment.GetEnvironmentVariable("HANGFIRE_PASSWORD") ?? "password";

        public static void Bind(IConfiguration configuration)
        {
            if (configuration == null) return;
            var section = configuration.GetSection("HangfireDashboard");
            if (section.Exists())
            {
                Username = section.GetValue<string>("Username", Username);
                Password = section.GetValue<string>("Password", Password);
            }
        }
    }
}
