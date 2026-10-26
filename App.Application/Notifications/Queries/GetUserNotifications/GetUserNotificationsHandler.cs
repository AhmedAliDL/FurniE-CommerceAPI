using App.Application.Common.Enums;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.Services;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Notifications.Dto;
using App.Application.Responses;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Notifications.Queries.GetUserNotifications
{
    public class GetUserNotificationsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<GetUserNotificationsHandler> logger) : IRequestHandler<GetUserNotificationsQuery, PagedResult<UserNotificationDto>>
    {
        public async Task<PagedResult<UserNotificationDto>> Handle(GetUserNotificationsQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get user notifications operation started.");
            Guid userId = currentUserService.UserId ?? throw new NotFoundException("User not found.");

            (IReadOnlyList<Notification> notifications, int totalCount) = await unitOfWork.Notifications.FindPagedAsync(
                n => n.ReceiverId == userId,
                [],
                SortDirection.Descending,
                n => n.CreatedAt,
                request.Page,
                request.PageSize,
                cancellationToken);

            var items = notifications.Select(n => new UserNotificationDto
            {
                NotificationId = n.Id,
                Message = n.Message,
                SenderId = n.SenderId,
                Link = n.Link,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
            }).ToList();

            logger.LogInformation("Get user notifications operation completed. TotalCount: {TotalCount}", totalCount);
            return new PagedResult<UserNotificationDto>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
