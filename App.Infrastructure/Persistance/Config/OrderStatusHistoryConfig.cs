using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistance.Config
{
    public class OrderStatusHistoryConfig : IEntityTypeConfiguration<OrderStatusHistory>
    {
        public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
        {
            builder.ToTable("OrderStatusHistory");
            builder.HasKey(h => h.Id);
            builder.Property(h => h.Id).ValueGeneratedOnAdd();

            builder.Property(h => h.FromStatus).IsRequired().HasMaxLength(30);
            builder.Property(h => h.ToStatus).IsRequired().HasMaxLength(30);
            builder.Property(h => h.ChangedAt).HasColumnType("datetime2");

            // Rows are immutable; deleting the order removes its timeline.
            builder.HasOne(h => h.Order)
                .WithMany()
                .HasForeignKey(h => h.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(h => new { h.OrderId, h.ChangedAt });
        }
    }
}
