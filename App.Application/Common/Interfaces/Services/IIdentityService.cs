using App.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace App.Application.Common.Interfaces.Services
{
    public interface IIdentityService
    {
        Task<List<AppUser>> GetUsersByIdsAsync(List<Guid> ids);
        Task<List<AppUser>> GetUsersInRoleAsync(string role);
        Task<(List<AppUser> Users, int TotalCount)> GetUsersPagedAsync(string? search, int page, int pageSize);
        Task<AppUser?> GetUserByIdAsync(Guid userId);
        Task<AppUser?> GetUserByEmailAsync(string email);
        Task<IdentityResult> CreateUserAsync(AppUser user, string password);
        Task<IdentityResult> UpdateUserAsync(AppUser user);

        bool CheckEmailUniqueness(string email);
        Task<string> GenerateEmailConfirmationTokenAsync(AppUser user);
        Task<bool> CheckEmailConfirmationTokenAsync(AppUser user, string token);

        Task<bool> ChangeUserPasswordAsync(AppUser user, string oldPassword, string newPassword);
        Task<string> GenerateForgetPasswordConfirmationTokenAsync(AppUser user);
        Task<bool> ResetUserPasswordAsync(AppUser user, string token, string newPassword);
        Task<bool> CheckCorrectnessOfPasswordAsync(AppUser user, string password);
        Task<bool> ConfirmUserPhoneAsync(AppUser user);
    }
}
