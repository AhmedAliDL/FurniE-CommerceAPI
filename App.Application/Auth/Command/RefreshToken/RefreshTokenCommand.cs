using App.Application.Auth.Dto;
using App.Application.Responses;
using MediatR;

namespace App.Application.Auth.Command.RefreshToken
{
    public record RefreshTokenCommand(string RefreshToken) : IRequest<ResponseResult<RefreshTokenDto>>;
}
