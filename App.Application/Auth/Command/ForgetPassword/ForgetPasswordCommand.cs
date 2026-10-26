using App.Application.Notifications.Dto;
using MediatR;

namespace App.Application.Auth.Command.ForgetPassword
{
    public record ForgetPasswordCommand(string Email) : IRequest<NotifyDto>;
}
