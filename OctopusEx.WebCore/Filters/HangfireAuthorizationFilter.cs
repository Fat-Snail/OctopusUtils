using Hangfire.Annotations;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace OctopusEx.WebCore.Filters;


public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public String Username { get; set; } = "admin";
    public String Password { get; set; } = "password";

    public Boolean Authorize([NotNull] DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        var header = httpContext.Request.Headers["Authorization"];

        if ( String.IsNullOrWhiteSpace(header) )
        {
            SetChallengeResponse(httpContext);
            return false;
        }

        var authValues = System.Net.Http.Headers.AuthenticationHeaderValue.Parse(header);

        if ( !"Basic".Equals(authValues.Scheme, StringComparison.InvariantCultureIgnoreCase) )
        {
            SetChallengeResponse(httpContext);
            return false;
        }

        var parameter = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(authValues.Parameter));
        var parts = parameter.Split(':');

        if ( parts.Length < 2 )
        {
            SetChallengeResponse(httpContext);
            return false;
        }

        var username = parts[0];
        var password = parts[1];

        if ( String.IsNullOrWhiteSpace(username) || String.IsNullOrWhiteSpace(password) )
        {
            SetChallengeResponse(httpContext);
            return false;
        }

        if ( username == Username && password == Password )
        {
            return true;
        }

        SetChallengeResponse(httpContext);
        return false;
    }

    private void SetChallengeResponse(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = 401;
        httpContext.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Hangfire Dashboard\"");
        httpContext.Response.WriteAsync("Authentication is required.");
    }
}
