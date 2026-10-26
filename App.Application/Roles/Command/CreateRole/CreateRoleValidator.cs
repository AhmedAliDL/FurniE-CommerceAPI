using FluentValidation;

namespace App.Application.Roles.Command.CreateRole
{
    public class CreateRoleValidator : AbstractValidator<CreateRoleCommand>
    {
        public CreateRoleValidator()
        {
            RuleFor(r => r.RoleName)
                .NotEmpty()
                .MaximumLength(100)
                .WithMessage("Role name is required and must not exceed 100 characters.");
            RuleFor(r => r.RoleDescription)
                .MaximumLength(255)
                .WithMessage("Role description must not exceed 255 characters.");
        }
    }
}
