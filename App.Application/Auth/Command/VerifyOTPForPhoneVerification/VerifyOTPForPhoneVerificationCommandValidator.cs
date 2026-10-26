using FluentValidation;

namespace App.Application.Auth.Command.VerifyOTPForPhoneVerification
{
    public class VerifyOTPForPhoneVerificationCommandValidator : AbstractValidator<VerifyOTPForPhoneVerificationCommand>
    {
        public VerifyOTPForPhoneVerificationCommandValidator()
        {
            RuleFor(x => x.OTP)
                .NotEmpty().WithMessage("OTP is required.")
                .Matches(@"^\d{6}$").WithMessage("OTP must be 6 digits long.");
        }
    }
}
