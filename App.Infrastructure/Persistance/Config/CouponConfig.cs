using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistance.Config
{
    public class CouponConfig : IEntityTypeConfiguration<Coupon>
    {
        public void Configure(EntityTypeBuilder<Coupon> builder)
        {
            builder.HasKey(c => c.CouponId);
            builder.Property(c => c.CouponId).ValueGeneratedOnAdd();

            builder.Property(c => c.CouponCode).HasColumnType("nvarchar(100)");

            builder.Property(c => c.CouponRatio).HasPrecision(5, 2);

            builder.HasQueryFilter(c => !c.IsDeleted);
        }
    }
}
