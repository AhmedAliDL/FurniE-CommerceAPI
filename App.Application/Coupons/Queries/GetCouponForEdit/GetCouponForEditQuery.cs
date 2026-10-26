using App.Application.Coupons.Dto;
using MediatR;

namespace App.Application.Coupons.Queries.GetCouponForEdit
{
    public record GetCouponForEditQuery(Guid CouponId) : IRequest<EditCouponDto>;
}
