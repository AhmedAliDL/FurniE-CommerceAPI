using App.Application.Common.Interfaces.Services;
using App.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Services
{
    public class RoleService(RoleManager<IdentityRole<Guid>> roleManager, UserManager<AppUser> userManager) : IRoleService
    {

        public async Task<List<IdentityRole<Guid>>> GetAllRolesAsync()
        {
            var roles = await roleManager.Roles.ToListAsync();

            return roles;

        }
        public async Task<IList<string>> GetAllRolesOfUser(AppUser user)
        {
            return await userManager.GetRolesAsync(user);
        }
        public async Task<bool> IsRoleExistsAsync(string roleName)
        {
            return await roleManager.RoleExistsAsync(roleName);
        }
        public async Task<IdentityResult> CreateRoleAsync(string roleName, string roleDescription)
        {

            var role = new IdentityRole<Guid>
            {
                Name = roleName
            };
            var result = await roleManager.CreateAsync(role);
            return result;
        }
        public async Task<IdentityResult> AssignRoleToUserAsync(AppUser user, string roleName)
        {

            var result = await userManager.AddToRoleAsync(user, roleName);
            return result;
        }
        public async Task<bool> DeleteAssignRoleAsync(AppUser user, string roleName)
        {
            var result = await userManager.RemoveFromRoleAsync(user, roleName);
            return result.Succeeded;
        }
        public async Task<IdentityRole<Guid>?> GetRoleAsync(string roleName)
        {
            return await roleManager.FindByNameAsync(roleName);
        }
        public async Task<bool> DeleteRoleAsync(IdentityRole<Guid> role)
        {

            var result = await roleManager.DeleteAsync(role);
            return result.Succeeded;
        }

    }
}
