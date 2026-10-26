using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Coupons.Queries.CheckCoupon
{
    public class CheckCouponHandler(IUnitOfWork unitOfWork, ILogger<CheckCouponHandler> logger) : IRequestHandler<CheckCouponQuery, ResponseResult<decimal>>
    {
        public async Task<ResponseResult<decimal>> Handle(CheckCouponQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Check coupon operation started.");

            if (string.IsNullOrEmpty(request.CouponCode))
                return ResponseResult<decimal>.Failure("Invalid coupon");

            var foundCoupon = await unitOfWork.Coupons.FindAsync(c => c.CouponCode == request.CouponCode, cancellationToken);
            if (foundCoupon == null)
                return ResponseResult<decimal>.Failure("Coupon not found");

            // ExpireDate is written from DateTime.Now by the coupon commands, so compare on the same clock.
            if (foundCoupon.ExpireDate < DateTime.Now)
                return ResponseResult<decimal>.Failure("Coupon has expired");

            logger.LogInformation("Check coupon operation completed.");
            return ResponseResult<decimal>.Success(foundCoupon.CouponRatio);
        }
    }
}
