using App.Application.Common.Interfaces.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Coupons.Command.UpdateCoupon
{
    public class UpdateCouponHandler(IUnitOfWork unitOfWork, ILogger<UpdateCouponHandler> logger) : IRequestHandler<UpdateCouponCommand, bool>
    {
        public async Task<bool> Handle(UpdateCouponCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Update coupon {CouponId} operation started.", request.CouponId);

            var coupon = await unitOfWork.Coupons.FindAsync(c => c.CouponId == request.CouponId, cancellationToken);
            if (coupon == null)
                return false;

            coupon.CouponCode = request.CouponCode ?? coupon.CouponCode;
            coupon.CouponRatio = request.CouponRatio ?? coupon.CouponRatio;
            coupon.ExpireDate = DateTime.Now
                .AddDays(request.PeriodInDays ?? 0)
                .AddHours(request.PeriodInHours ?? 0)
                .AddMinutes(request.PeriodInMinutes ?? 0);

            unitOfWork.Coupons.Update(coupon);
            await unitOfWork.CompleteAsync(cancellationToken);
            logger.LogInformation("Update coupon {CouponId} operation completed.", request.CouponId);
            return true;
        }
    }
}
