using App.Application.Orders.Dto;
using App.Application.Responses;
using MediatR;

namespace App.Application.Orders.Queries.GetUserOrders
{
    public record GetUserOrdersQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<OrderShippingDto>>;
}
