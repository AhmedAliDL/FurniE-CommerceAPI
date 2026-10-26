using FluentValidation;

namespace App.Application.Notifications.Command.MakeNotificationRead
{
    public class MakeNotificationReadValidator : AbstractValidator<MakeNotificationReadCommand>
    {
        public MakeNotificationReadValidator()
        {
            RuleFor(n => n.NotificationId)
                .NotEmpty()
                .NotNull()
                .WithMessage("Notification id is required.");
        }
    }
}
