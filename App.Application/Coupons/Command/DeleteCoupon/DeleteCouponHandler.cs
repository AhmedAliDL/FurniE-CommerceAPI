using App.Application.Common.Interfaces.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Coupons.Command.DeleteCoupon
{
    public class DeleteCouponHandler(IUnitOfWork unitOfWork, ILogger<DeleteCouponHandler> logger) : IRequestHandler<DeleteCouponCommand, bool>
    {
        public async Task<bool> Handle(DeleteCouponCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Delete coupon {CouponId} operation started.", request.CouponId);

            var coupon = await unitOfWork.Coupons.FindAsync(c => c.CouponId == request.CouponId, cancellationToken);
            if (coupon == null)
                return false;

            unitOfWork.Coupons.Delete(coupon);
            await unitOfWork.CompleteAsync(cancellationToken);
            logger.LogInformation("Delete coupon {CouponId} operation completed.", request.CouponId);
            return true;
        }
    }
}
