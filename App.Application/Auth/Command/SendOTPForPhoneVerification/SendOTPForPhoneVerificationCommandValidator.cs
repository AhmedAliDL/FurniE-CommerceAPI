using FluentValidation;

namespace App.Application.Auth.Command.SendOTPForPhoneVerification
{
    public class SendOTPForPhoneVerificationCommandValidator : AbstractValidator<SendOTPForPhoneVerificationCommand>
    {
        public SendOTPForPhoneVerificationCommandValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Matches(@"^01[0,1,2,5]\d{8}$").WithMessage("Invalid phone number format.");
        }
    }
}
