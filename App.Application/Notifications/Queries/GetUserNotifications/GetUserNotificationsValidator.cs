using FluentValidation;

namespace App.Application.Notifications.Queries.GetUserNotifications
{
    public class GetUserNotificationsValidator : AbstractValidator<GetUserNotificationsQuery>
    {
        public GetUserNotificationsValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);
        }
    }
}
