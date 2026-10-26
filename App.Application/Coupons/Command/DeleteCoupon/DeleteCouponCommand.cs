using MediatR;

namespace App.Application.Coupons.Command.DeleteCoupon
{
    public record DeleteCouponCommand(Guid CouponId) : IRequest<bool>;
}
