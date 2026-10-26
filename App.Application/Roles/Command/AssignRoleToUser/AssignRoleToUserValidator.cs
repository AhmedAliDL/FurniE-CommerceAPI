using FluentValidation;

namespace App.Application.Roles.Command.AssignRoleToUser
{
    public class AssignRoleToUserValidator : AbstractValidator<AssignRoleToUserCommand>
    {
        public AssignRoleToUserValidator()
        {
            RuleFor(a => a.RoleName)
                .NotEmpty()
                .MaximumLength(100)
                .WithMessage("Role name is required and must not exceed 100 characters.");
            RuleFor(a => a.UserEmail)
                .NotEmpty()
                .EmailAddress()
                .WithMessage("A valid user email is required.");
        }
    }
}
