using System.Text.Json.Serialization;
using App.Domain.Enums;
using MediatR;

namespace App.Application.Orders.Command.UpdateOrderStatus
{
    public record UpdateOrderStatusCommand(
        Guid OrderId,
        [property: JsonConverter(typeof(JsonStringEnumConverter))] OrderStatus Status) : IRequest;
}
