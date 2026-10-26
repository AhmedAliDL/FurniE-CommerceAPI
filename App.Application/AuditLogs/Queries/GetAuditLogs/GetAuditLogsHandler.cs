using App.Application.AuditLogs.Dto;
using App.Application.Common.Enums;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.AuditLogs.Queries.GetAuditLogs
{
    public class GetAuditLogsHandler(IUnitOfWork unitOfWork, ILogger<GetAuditLogsHandler> logger) : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
    {
        public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get audit logs operation started.");

            var (logs, totalCount) = await unitOfWork.AuditLogs.FindPagedAsync(
                l => request.EntityName == null || l.EntityName == request.EntityName,
                [l => l.User],
                SortDirection.Descending,
                l => l.CreatedAt,
                request.Page,
                request.PageSize,
                cancellationToken);

            var items = logs.Select(l => new AuditLogDto
            {
                Id = l.Id,
                UserId = l.UserId,
                UserEmail = l.User?.Email ?? string.Empty,
                Action = l.Action,
                EntityName = l.EntityName,
                EntityId = l.EntityId,
                OldValues = l.OldValues,
                NewValues = l.NewValues,
                CreatedAt = l.CreatedAt
            }).ToList();

            logger.LogInformation("Get audit logs operation completed. {Count} entries returned.", items.Count);
            return new PagedResult<AuditLogDto>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
