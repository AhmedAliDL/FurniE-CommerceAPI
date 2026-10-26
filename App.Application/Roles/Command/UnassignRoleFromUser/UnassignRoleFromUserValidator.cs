using FluentValidation;

namespace App.Application.Roles.Command.UnassignRoleFromUser
{
    public class UnassignRoleFromUserValidator : AbstractValidator<UnassignRoleFromUserCommand>
    {
        public UnassignRoleFromUserValidator()
        {
            RuleFor(c => c.RoleName)
                .NotEmpty()
                .MaximumLength(256)
                .WithMessage("Role name is required and must not exceed 256 characters.");
            RuleFor(c => c.UserEmail)
                .NotEmpty()
                .EmailAddress()
                .WithMessage("A valid user email is required.");
        }
    }
}
