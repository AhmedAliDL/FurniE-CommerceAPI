using App.Application.Common.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;


namespace App.Infrastructure.Services
{
    public class CurrentUserService(IHttpContextAccessor httpContextAccessor)
     : ICurrentUserService
    {
        public Guid? UserId
        {
            get
            {
                var value = httpContextAccessor.HttpContext?
                    .User?
                    .FindFirstValue(ClaimTypes.NameIdentifier);

                return Guid.TryParse(value, out var userId)
                    ? userId
                    : null;
            }
        }

        public bool IsInRole(string role)
        {
            return httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;
        }
    }
}
