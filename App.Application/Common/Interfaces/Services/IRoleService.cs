using App.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace App.Application.Common.Interfaces.Services
{
    public interface IRoleService
    {

        Task<List<IdentityRole<Guid>>> GetAllRolesAsync();
        Task<IList<string>> GetAllRolesOfUser(AppUser user);
        Task<IdentityRole<Guid>?> GetRoleAsync(string roleName);
        Task<IdentityResult> CreateRoleAsync(string roleName, string roleDescription);
        Task<IdentityResult> AssignRoleToUserAsync(AppUser user, string roleName);
        Task<bool> DeleteRoleAsync(IdentityRole<Guid> role);
        Task<bool> DeleteAssignRoleAsync(AppUser user, string roleName);
        Task<bool> IsRoleExistsAsync(string roleName);

    }
}
