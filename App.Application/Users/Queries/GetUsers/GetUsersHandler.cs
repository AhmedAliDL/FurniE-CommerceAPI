using App.Application.Common.Interfaces.Services;
using App.Application.Responses;
using App.Application.Users.Dto;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Users.Queries.GetUsers
{
    public class GetUsersHandler(IIdentityService identityService, IRoleService roleService, ILogger<GetUsersHandler> logger) : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
    {
        public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get users operation started.");

            var (users, totalCount) = await identityService.GetUsersPagedAsync(request.Search, request.Page, request.PageSize);

            var items = new List<UserDto>(users.Count);
            foreach (var user in users)
            {
                var roles = await roleService.GetAllRolesOfUser(user);
                items.Add(new UserDto
                {
                    UserId = user.Id,
                    Email = user.Email ?? string.Empty,
                    FName = user.FName,
                    LName = user.LName,
                    PhoneNumber = user.PhoneNumber,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    Roles = roles.ToList()
                });
            }

            logger.LogInformation("Get users operation completed. {Count} users returned.", items.Count);
            return new PagedResult<UserDto>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
