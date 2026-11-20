using App.Application.Common.Interfaces.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Orders.Command.SetOrderErpReference
{
    public class SetOrderErpReferenceHandler(IUnitOfWork unitOfWork, ILogger<SetOrderErpReferenceHandler> logger) : IRequestHandler<SetOrderErpReferenceCommand, bool>
    {
        public async Task<bool> Handle(SetOrderErpReferenceCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Set order {OrderId} ERP reference operation started.", request.OrderId);

            var order = await unitOfWork.Orders.FindAsync(o => o.OrderId == request.OrderId, cancellationToken);
            if (order is null)
            {
                logger.LogWarning("Order {OrderId} was not found.", request.OrderId);
                return false;
            }

            if (request.ErpOrderId is not null)
                order.ErpOrderId = request.ErpOrderId.Trim();
            if (request.ErpInvoiceId is not null)
                order.ErpInvoiceId = request.ErpInvoiceId.Trim();

            order.UpdatedAt = DateTime.UtcNow;
            unitOfWork.Orders.Update(order);
            await unitOfWork.CompleteAsync(cancellationToken);

            logger.LogInformation("Set order {OrderId} ERP reference operation completed.", request.OrderId);
            return true;
        }
    }
}
