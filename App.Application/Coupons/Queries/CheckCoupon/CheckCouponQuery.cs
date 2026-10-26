using App.Application.Responses;
using MediatR;

namespace App.Application.Coupons.Queries.CheckCoupon
{
    public record CheckCouponQuery(string CouponCode) : IRequest<ResponseResult<decimal>>;
}
