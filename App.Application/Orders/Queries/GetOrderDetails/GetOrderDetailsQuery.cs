using App.Application.Orders.Dto;
using MediatR;

namespace App.Application.Orders.Queries.GetOrderDetails
{
    public record GetOrderDetailsQuery(Guid OrderId) : IRequest<List<OrderDetailsDto>>;
}
