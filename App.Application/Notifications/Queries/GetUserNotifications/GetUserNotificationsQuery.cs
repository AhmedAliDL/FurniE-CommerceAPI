using App.Application.Notifications.Dto;
using App.Application.Responses;
using MediatR;

namespace App.Application.Notifications.Queries.GetUserNotifications
{
    public record GetUserNotificationsQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<UserNotificationDto>>;
}
