using App.Application.AuditLogs.Dto;
using App.Application.Responses;
using MediatR;

namespace App.Application.AuditLogs.Queries.GetAuditLogs
{
    public record GetAuditLogsQuery : IRequest<PagedResult<AuditLogDto>>
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public string? EntityName { get; init; }
    }
}
