using App.Application.Common.Interfaces.Services;
using FluentValidation;

namespace App.Application.Auth.Command.EditProfile
{
    public class EditProfileValidator : AbstractValidator<EditProfileCommand>
    {
        private readonly IIdentityService _identityService;

        public EditProfileValidator(IIdentityService identityService)
        {
            _identityService = identityService;
            RuleFor(p => p.FName)
                .MaximumLength(30)
                .WithMessage("First Name must be between 3 and 30 characters.")
                .MinimumLength(3)
                .WithMessage("First Name must be between 3 and 30 characters.");
            RuleFor(p => p.LName)
                .MaximumLength(30)
                .WithMessage("Last Name must be between 3 and 30 characters.")
                .MinimumLength(3)
                .WithMessage("Last Name must be between 3 and 30 characters.");
            RuleFor(p => p.Phone)
                .Matches(@"^01[0125]\d{8}$")
                .WithMessage("Invalid egyptian phone number.");
            RuleFor(p => p.Email)
                .EmailAddress()
                .WithMessage("Invalid email address.")
                .Must(email => !_identityService.CheckEmailUniqueness(email!))
                .WithMessage("This Email Address has been used before.")
                .When(p => !string.IsNullOrWhiteSpace(p.Email));
            RuleFor(p => p.ConfirmNewPassword)
                .Equal(p => p.NewPassword)
                .WithMessage("Confirmed Password field must match New Password field");


        }
    }
}
