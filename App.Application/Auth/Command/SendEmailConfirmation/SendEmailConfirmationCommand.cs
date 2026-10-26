using App.Application.Notifications.Dto;
using MediatR;

namespace App.Application.Auth.Command.SendEmailConfirmation
{
    public record SendEmailConfirmationCommand(string Email) : IRequest<NotifyDto>;
}
