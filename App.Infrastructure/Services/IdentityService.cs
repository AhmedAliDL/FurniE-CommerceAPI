using App.Application.Common.Interfaces.Services;
using App.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace App.Infrastructure.Services
{
    public class IdentityService(UserManager<AppUser> userManager) : IIdentityService
    {
        public async Task<IdentityResult> CreateUserAsync(AppUser user, string password)
        {
            return await userManager.CreateAsync(user, password);
        }
        public async Task<List<AppUser>> GetUsersByIdsAsync(List<Guid> ids)
        {
            return await userManager.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
        }
        public async Task<List<AppUser>> GetUsersInRoleAsync(string role)
        {
            return [.. await userManager.GetUsersInRoleAsync(role)];
        }
        public async Task<(List<AppUser> Users, int TotalCount)> GetUsersPagedAsync(string? search, int page, int pageSize)
        {
            var query = userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(u =>
                    (u.Email != null && u.Email.Contains(term)) ||
                    u.FName.Contains(term) ||
                    u.LName.Contains(term) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
            }

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, totalCount);
        }
        public async Task<AppUser?> GetUserByEmailAsync(string email)
        {
            return await userManager.FindByEmailAsync(email);
        }
        public async Task<AppUser?> GetUserByIdAsync(Guid userId)
        {
            return await userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }
        public async Task<bool> CheckCorrectnessOfPasswordAsync(AppUser user, string password)
        {
            return await userManager.CheckPasswordAsync(user, password);
        }
        public async Task<IdentityResult> UpdateUserAsync(AppUser user)
        {
            return await userManager.UpdateAsync(user);
        }
        public bool CheckEmailUniqueness(string email)
        {
            return userManager.Users.Any(u => u.Email == email);
        }
        public async Task<string> GenerateEmailConfirmationTokenAsync(AppUser user)
        {
            return await userManager.GenerateEmailConfirmationTokenAsync(user);
        }
        public async Task<string> GenerateForgetPasswordConfirmationTokenAsync(AppUser user)
        {
            return await userManager.GeneratePasswordResetTokenAsync(user);
        }
        public async Task<bool> CheckEmailConfirmationTokenAsync(AppUser user, string token)
        {
            var isValid = await userManager.ConfirmEmailAsync(user, token);
            return isValid.Succeeded;
        }
        public async Task<bool> ChangeUserPasswordAsync(AppUser user, string oldPassword, string newPassword)
        {
            var changed = await userManager.ChangePasswordAsync(user, oldPassword, newPassword);
            return changed.Succeeded;
        }
        public async Task<bool> ResetUserPasswordAsync(AppUser user, string token, string newPassword)
        {
            var changed = await userManager.ResetPasswordAsync(user, token, newPassword);
            return changed.Succeeded;
        }
        public async Task<bool> ConfirmUserPhoneAsync(AppUser user)
        {
            user.PhoneNumberConfirmed = true;
            var result = await userManager.UpdateAsync(user);
            return result.Succeeded;
        }
    }

}
