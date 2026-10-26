namespace App.Application.Common.Interfaces.Services
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        bool IsInRole(string role);
    }
}