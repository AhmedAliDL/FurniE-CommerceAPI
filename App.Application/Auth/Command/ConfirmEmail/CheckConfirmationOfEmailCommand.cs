using MediatR;
using Microsoft.AspNetCore.Identity;

namespace App.Application.Auth.Command.ConfirmEmail
{
    public record ConfirmEmailCommand(Guid UserId, string ConfirmationToken) : IRequest<IdentityResult>;
}
