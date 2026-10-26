using App.Application.Common.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace App.Infrastructure.Services
{
    public class ShippingRateService(IConfiguration configuration) : IShippingRateService
    {
        private const decimal FallbackFee = 120m;

        public decimal GetShippingFee(string? city)
        {
            if (!string.IsNullOrWhiteSpace(city))
            {
                var zones = configuration.GetSection("ShippingRates:Zones").Get<ShippingZoneOption[]>() ?? [];
                foreach (var zone in zones)
                {
                    if (zone.Cities.Any(c => string.Equals(Normalize(c), Normalize(city), StringComparison.OrdinalIgnoreCase)))
                        return zone.Fee;
                }
            }

            return configuration.GetValue<decimal?>("ShippingRates:DefaultFee") ?? FallbackFee;
        }

        private static string Normalize(string value) =>
            string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public class ShippingZoneOption
    {
        public decimal Fee { get; set; }
        public string[] Cities { get; set; } = [];
    }
}
