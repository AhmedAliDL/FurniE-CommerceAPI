using MediatR;

namespace App.Application.Auth.Command.VerifyOTPForPhoneVerification
{
    public record VerifyOTPForPhoneVerificationCommand(string OTP) : IRequest<string>;
}
