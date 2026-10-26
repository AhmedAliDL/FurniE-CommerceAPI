using App.Application.Common.Enums;
using App.Application.Common.Interfaces.UnitOfWork;
using App.Application.Coupons.Dto;
using App.Application.Responses;
using App.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Application.Coupons.Queries.GetAllCoupons
{
    public class GetAllCouponsHandler(IUnitOfWork unitOfWork, ILogger<GetAllCouponsHandler> logger) : IRequestHandler<GetAllCouponsQuery, PagedResult<CouponDto>>
    {
        public async Task<PagedResult<CouponDto>> Handle(GetAllCouponsQuery request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Get all coupons operation started.");

            (IReadOnlyList<Coupon> coupons, int totalCount) = await unitOfWork.Coupons.FindPagedAsync(
                c => true,
                [],
                SortDirection.Descending,
                c => c.CreatedAt,
                request.Page,
                request.PageSize,
                cancellationToken);

            logger.LogInformation("Get all coupons operation completed. {Count} coupons returned.", totalCount);
            return new PagedResult<CouponDto>
            {
                Items = coupons.Select(ToDto).ToList(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        private static CouponDto ToDto(Coupon coupon) => new()
        {
            CouponId = coupon.CouponId,
            CouponCode = coupon.CouponCode,
            CouponRatio = coupon.CouponRatio,
            ExpireDate = coupon.ExpireDate
        };
    }
}
