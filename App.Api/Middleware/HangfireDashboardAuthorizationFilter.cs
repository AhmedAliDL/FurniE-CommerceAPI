using Hangfire;
using Hangfire.Dashboard;
using System.Net;

namespace App.Api.Middleware
{
    /// <summary>
    /// Restricts the Hangfire dashboard to authenticated administrators and loopback requests.
    /// </summary>
    public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        /// <summary>
        /// Determines whether the current request may access the Hangfire dashboard.
        /// </summary>
        /// <param name="context">The Hangfire dashboard context.</param>
        /// <returns><see langword="true"/> when the request is authorized; otherwise, <see langword="false"/>.</returns>
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            if (httpContext.User.Identity?.IsAuthenticated == true && httpContext.User.IsInRole("Admin"))
            {
                return true;
            }

            return httpContext.Connection.RemoteIpAddress is { } remoteIp && IPAddress.IsLoopback(remoteIp);
        }
    }
}
