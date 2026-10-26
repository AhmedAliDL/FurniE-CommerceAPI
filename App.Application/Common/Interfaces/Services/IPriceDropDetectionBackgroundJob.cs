namespace App.Application.Common.Interfaces.Services
{
    public interface IPriceDropDetectionBackgroundJob
    {
        public Task ExecuteAsync(List<Guid> customerIds);
    }
}
