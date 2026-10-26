using App.Application.Common.Interfaces.UnitOfWork;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Coupons.Command.CreateCoupon
{
    public class CreateCouponHandler(IUnitOfWork unitOfWork, ILogger<CreateCouponHandler> logger) : IRequestHandler<CreateCouponCommand, Guid>
    {
        public async Task<Guid> Handle(CreateCouponCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Create coupon operation started.");
            var coupon = new Coupon
            {
                CouponCode = request.CouponCode,
                CouponRatio = request.CouponRatio,
                ExpireDate = DateTime.Now
                    .AddDays(request.PeriodInDays)
                    .AddHours(request.PeriodInHours)
                    .AddMinutes(request.PeriodInMinutes)
            };
            await unitOfWork.Coupons.AddAsync(coupon, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
            logger.LogInformation("Create coupon operation completed.");
            return coupon.CouponId;
        }
    }
}
