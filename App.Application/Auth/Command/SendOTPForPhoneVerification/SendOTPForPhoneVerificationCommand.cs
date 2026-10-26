using MediatR;

namespace App.Application.Auth.Command.SendOTPForPhoneVerification
{
    public record SendOTPForPhoneVerificationCommand(string PhoneNumber) : IRequest<string>;
}
