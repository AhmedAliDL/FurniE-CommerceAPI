using App.Application.Responses;
using App.Application.Users.Dto;
using MediatR;

namespace App.Application.Users.Queries.GetUsers
{
    public record GetUsersQuery : IRequest<PagedResult<UserDto>>
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public string? Search { get; init; }
    }
}
