using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Coupons.Dto;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Coupons.Queries.GetCouponForEdit
{
    public class GetCouponForEditHandler(IUnitOfWork unitOfWork, ILogger<GetCouponForEditHandler> logger) : IRequestHandler<GetCouponForEditQuery, EditCouponDto>
    {
        public async Task<EditCouponDto> Handle(GetCouponForEditQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get coupon {CouponId} for edit operation started.", request.CouponId);

            var coupon = await unitOfWork.Coupons.FindAsync(c => c.CouponId == request.CouponId, cancellationToken)
                ?? throw new NotFoundException($"Coupon with id {request.CouponId} was not found.");

            TimeSpan period = coupon.ExpireDate - DateTime.Now;

            logger.LogInformation("Get coupon {CouponId} for edit operation completed.", request.CouponId);
            return new EditCouponDto
            {
                CouponId = coupon.CouponId,
                CouponCode = coupon.CouponCode,
                CouponRatio = coupon.CouponRatio,
                PeriodInDays = (int)period.TotalDays,
                PeriodInHours = (int)period.TotalHours,
                PeriodInMinutes = (int)period.TotalMinutes
            };
        }
    }
}
