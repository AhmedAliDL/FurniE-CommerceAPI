namespace App.Application.Common.Interfaces.Services
{
    public interface IShippingRateService
    {
        decimal GetShippingFee(string? city);
    }
}
