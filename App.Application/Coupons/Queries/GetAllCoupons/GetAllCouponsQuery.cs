using App.Application.Coupons.Dto;
using App.Application.Responses;
using MediatR;

namespace App.Application.Coupons.Queries.GetAllCoupons
{
    public record GetAllCouponsQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<CouponDto>>;
}
