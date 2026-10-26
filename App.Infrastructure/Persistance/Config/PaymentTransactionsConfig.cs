using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistance.Config
{
    public class PaymentTransactionsConfig : IEntityTypeConfiguration<PaymentTransaction>
    {
        public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).ValueGeneratedOnAdd();

            builder.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            builder.Property(t => t.RefundedAmount).HasColumnType("decimal(18,2)");
            builder.Property(t => t.MerchantCommission).HasColumnType("decimal(18,2)");
            builder.Property(t => t.PaymobTransactionId).HasColumnType("bigint");
            builder.Property(t => t.PaymobOrderId).HasColumnType("bigint");
            builder.Property(t => t.Currency).HasColumnType("nvarchar(10)");
            builder.Property(t => t.Status).HasColumnType("nvarchar(30)");
            builder.Property(t => t.PaymentMethod).HasColumnType("nvarchar(50)");
            builder.Property(t => t.SourceType).HasColumnType("nvarchar(30)");
            builder.Property(t => t.ErrorMessage).HasColumnType("nvarchar(400)");
            builder.Property(t => t.ResponseCode).HasColumnType("nvarchar(20)");
            builder.Property(t => t.PaymobCreatedAt).HasColumnType("datetime2");
            builder.Property(t => t.RawPayload).HasColumnType("nvarchar(max)");

            builder.HasIndex(t => t.PaymobTransactionId);
            builder.HasIndex(t => t.OrderId);

            builder.HasOne(t => t.Order)
                .WithMany(o => o.PaymentTransactions)
                .OnDelete(DeleteBehavior.NoAction)
                .HasForeignKey(t => t.OrderId);

            builder.HasQueryFilter(t => !t.IsDeleted);
        }
    }
}
