using FluentValidation;

namespace App.Application.Roles.Command.DeleteRole
{
    public class DeleteRoleValidator : AbstractValidator<DeleteRoleCommand>
    {
        public DeleteRoleValidator()
        {
            RuleFor(c => c.RoleName)
                .NotEmpty()
                .MaximumLength(256)
                .WithMessage("Role name is required and must not exceed 256 characters.");
        }
    }
}
