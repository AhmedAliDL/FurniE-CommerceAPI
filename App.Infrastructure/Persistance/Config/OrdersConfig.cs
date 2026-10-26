using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistance.Config
{
    public class OrdersConfig : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(o => o.OrderId);
            builder.Property(o => o.OrderId).ValueGeneratedOnAdd();
            builder.Property(o => o.TotalPrice).HasColumnType("decimal(18,2)");
            builder.Property(o => o.ShippingFee).HasColumnType("decimal(18,2)");
            builder.Property(o => o.CouponDiscount).HasColumnType("decimal(18,2)");
            builder.Property(o => o.OrderStatus).HasConversion<string>().HasColumnType("nvarchar(30)");
            builder.Property(o => o.ShippingAddress).HasColumnType("nvarchar(450)");


            builder.HasOne(o => o.Customer)
                .WithMany(u => u.Orders)
                .OnDelete(DeleteBehavior.NoAction)
                .HasForeignKey(o => o.UserId);

            builder.HasQueryFilter(o => !o.IsDeleted);
        }
    }
}
