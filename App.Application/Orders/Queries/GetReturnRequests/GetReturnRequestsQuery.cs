using App.Application.Orders.Dto;
using App.Application.Responses;
using App.Domain.Enums;
using MediatR;

namespace App.Application.Orders.Queries.GetReturnRequests
{
    public record GetReturnRequestsQuery(ReturnRequestStatus? Status = null, int Page = 1, int PageSize = 20) : IRequest<ResponseResult<PagedResult<ReturnRequestListDto>>>;
}
