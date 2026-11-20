using App.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace App.Api.Health
{
    /// <summary>
    /// Anonymous readiness probe reporting database and Redis connectivity for load balancers and the ERP.
    /// </summary>
    public static class HealthEndpoint
    {
        public static async Task<IResult> HandleAsync(IServiceProvider services, CancellationToken cancellationToken)
        {
            var checks = new List<object>();
            var healthy = true;

            try
            {
                var db = services.GetRequiredService<AppDbContext>();
                var connected = await db.Database.CanConnectAsync(cancellationToken);
                healthy &= connected;
                checks.Add(new { name = "database", status = connected ? "Healthy" : "Unhealthy", error = connected ? null : "Cannot connect to the database." });
            }
            catch (Exception ex)
            {
                healthy = false;
                checks.Add(new { name = "database", status = "Unhealthy", error = ex.Message });
            }

            try
            {
                var redis = services.GetRequiredService<IConnectionMultiplexer>();
                var ping = await redis.GetDatabase().PingAsync();
                checks.Add(new { name = "redis", status = "Healthy", error = (string?)null, latencyMs = ping.TotalMilliseconds });
            }
            catch (Exception ex)
            {
                healthy = false;
                checks.Add(new { name = "redis", status = "Unhealthy", error = ex.Message, latencyMs = (double?)null });
            }

            return Results.Json(
                new { status = healthy ? "Healthy" : "Unhealthy", timestamp = DateTime.UtcNow, checks },
                statusCode: healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
        }
    }
}
