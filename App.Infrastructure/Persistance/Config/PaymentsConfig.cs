using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistance.Config
{
    public class PaymentsConfig : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(p => p.PaymentId);
            builder.Property(p => p.PaymentId).ValueGeneratedOnAdd();
            builder.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            builder.Property(p => p.PaymentMethod).HasColumnType("nvarchar(50)");
            builder.Property(p => p.PaymentStatus).HasColumnType("nvarchar(50)");

            builder.HasOne(p => p.Order)
                .WithOne(o => o.Payment)
                .OnDelete(DeleteBehavior.SetNull)
                .HasForeignKey<Payment>(p => p.OrderId)
                .IsRequired(false);

            builder.HasQueryFilter(p => !p.IsDeleted);
        }
    }
}
