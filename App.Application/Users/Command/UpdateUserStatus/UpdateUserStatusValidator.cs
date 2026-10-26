using FluentValidation;

namespace App.Application.Users.Command.UpdateUserStatus
{
    public class UpdateUserStatusValidator : AbstractValidator<UpdateUserStatusCommand>
    {
        public UpdateUserStatusValidator()
        {
            RuleFor(c => c.UserId)
                .NotEmpty()
                .WithMessage("User id is required.");
        }
    }
}
