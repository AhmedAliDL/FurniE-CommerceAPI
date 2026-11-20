using MediatR;

namespace App.Application.Orders.Command.SetOrderErpReference
{
    public record SetOrderErpReferenceCommand : IRequest<bool>
    {
        public Guid OrderId { get; set; }
        public string? ErpOrderId { get; set; }
        public string? ErpInvoiceId { get; set; }
    }
}
