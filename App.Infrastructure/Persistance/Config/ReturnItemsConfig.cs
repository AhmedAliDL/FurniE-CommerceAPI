using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistance.Config
{
    public class ReturnItemsConfig : IEntityTypeConfiguration<ReturnItem>
    {
        public void Configure(EntityTypeBuilder<ReturnItem> builder)
        {
            builder.HasKey(i => i.Id);
            builder.Property(i => i.Id).ValueGeneratedOnAdd();
            builder.Property(i => i.ProductName).HasColumnType("nvarchar(200)");

            builder.HasOne(i => i.ReturnRequest)
                .WithMany(r => r.ReturnItems)
                .OnDelete(DeleteBehavior.Cascade)
                .HasForeignKey(i => i.ReturnRequestId);

            builder.HasOne(i => i.OrderItem)
                .WithMany()
                .OnDelete(DeleteBehavior.NoAction)
                .HasForeignKey(i => i.OrderItemId);

            builder.HasIndex(i => i.ReturnRequestId);

            builder.HasQueryFilter(i => !i.IsDeleted);
        }
    }
}
