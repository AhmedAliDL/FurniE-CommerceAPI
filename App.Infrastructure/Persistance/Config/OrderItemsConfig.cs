using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistance.Config
{
    public class OrderItemsConfig : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.HasKey(oi => oi.OrderItemId);
            builder.Property(oi => oi.OrderItemId).ValueGeneratedOnAdd();

            builder.Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)");
            builder.Property(oi => oi.Quantity).HasColumnType("int");

            builder.HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .OnDelete(DeleteBehavior.Cascade)
                .HasForeignKey(oi => oi.ProductId);

            builder.HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .OnDelete(DeleteBehavior.Cascade)
                .HasForeignKey(oi => oi.OrderId);

            builder.HasQueryFilter(oi => !oi.IsDeleted);
        }
    }
}
