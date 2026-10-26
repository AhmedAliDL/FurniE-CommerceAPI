using App.Application.Common.Enums;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Orders.Dto;
using App.Application.Responses;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Orders.Queries.GetReturnRequests
{
    public class GetReturnRequestsHandler(IUnitOfWork unitOfWork, ILogger<GetReturnRequestsHandler> logger) : IRequestHandler<GetReturnRequestsQuery, ResponseResult<PagedResult<ReturnRequestListDto>>>
    {
        public async Task<ResponseResult<PagedResult<ReturnRequestListDto>>> Handle(GetReturnRequestsQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get return requests operation started.");

            (IReadOnlyList<ReturnRequest> requests, int totalCount) = await unitOfWork.ReturnRequests.FindPagedAsync(
                r => request.Status == null || r.Status == request.Status,
                [r => r.Customer, r => r.ReturnItems],
                SortDirection.Descending,
                r => r.CreatedAt,
                request.Page,
                request.PageSize,
                cancellationToken);

            var items = requests
                .Select(r => new ReturnRequestListDto
                {
                    Id = r.Id,
                    OrderId = r.OrderId,
                    CustomerName = r.Customer == null ? null : $"{r.Customer.FName} {r.Customer.LName}".Trim(),
                    ReasonCategory = r.ReasonCategory.ToString(),
                    ReasonText = r.ReasonText,
                    Status = r.Status,
                    CourierName = r.CourierName,
                    CourierPhone = r.CourierPhone,
                    ReceivedAt = r.ReceivedAt,
                    RefundAmountCents = r.RefundAmountCents,
                    AdminNote = r.AdminNote,
                    CreatedAt = r.CreatedAt,
                    ProcessedAt = r.ProcessedAt,
                    Items = r.ReturnItems.Select(i => new ReturnItemListDto
                    {
                        Id = i.Id,
                        OrderItemId = i.OrderItemId,
                        ProductName = i.ProductName,
                        Quantity = i.Quantity
                    }).ToList()
                })
                .ToList();

            logger.LogInformation("Get return requests operation completed. TotalCount: {TotalCount}", totalCount);
            return ResponseResult<PagedResult<ReturnRequestListDto>>.Success(new PagedResult<ReturnRequestListDto>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            });
        }
    }
}
