using App.Application.Common.Enums;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Orders.Dto;
using App.Application.Responses;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Orders.Queries.GetCancellationRequests
{
    public class GetCancellationRequestsHandler(IUnitOfWork unitOfWork, ILogger<GetCancellationRequestsHandler> logger) : IRequestHandler<GetCancellationRequestsQuery, ResponseResult<PagedResult<CancellationRequestListDto>>>
    {
        public async Task<ResponseResult<PagedResult<CancellationRequestListDto>>> Handle(GetCancellationRequestsQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get cancellation requests operation started.");

            (IReadOnlyList<CancellationRequest> requests, int totalCount) = await unitOfWork.CancellationRequests.FindPagedAsync(
                r => (request.Status == null || r.Status == request.Status) && r.Order != null,
                [r => r.Order, r => r.Customer],
                SortDirection.Descending,
                r => r.CreatedAt,
                request.Page,
                request.PageSize,
                cancellationToken);

            var items = requests
                .Select(r => new CancellationRequestListDto
                {
                    Id = r.Id,
                    OrderId = r.OrderId,
                    CustomerName = r.Customer == null ? null : $"{r.Customer.FName} {r.Customer.LName}".Trim(),
                    Reason = r.Reason,
                    PreviousOrderStatus = r.PreviousOrderStatus,
                    Status = r.Status,
                    AdminNote = r.AdminNote,
                    CreatedAt = r.CreatedAt,
                    ProcessedAt = r.ProcessedAt
                })
                .ToList();

            logger.LogInformation("Get cancellation requests operation completed. TotalCount: {TotalCount}", totalCount);
            return ResponseResult<PagedResult<CancellationRequestListDto>>.Success(new PagedResult<CancellationRequestListDto>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            });
        }
    }
}
