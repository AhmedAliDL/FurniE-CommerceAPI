using App.Application.Responses;
using MediatR;

namespace App.Application.Auth.Command.Logout
{
    public record LogoutCommand(string RefreshToken) : IRequest<ResponseResult<bool>>;
}
