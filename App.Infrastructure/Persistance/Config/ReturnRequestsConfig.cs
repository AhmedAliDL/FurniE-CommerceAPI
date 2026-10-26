using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistance.Config
{
    public class ReturnRequestsConfig : IEntityTypeConfiguration<ReturnRequest>
    {
        public void Configure(EntityTypeBuilder<ReturnRequest> builder)
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).ValueGeneratedOnAdd();
            builder.Property(r => r.ReasonCategory).HasConversion<string>().HasColumnType("nvarchar(30)");
            builder.Property(r => r.ReasonText).HasColumnType("nvarchar(500)");
            builder.Property(r => r.Status).HasColumnType("nvarchar(30)");
            builder.Property(r => r.CourierName).HasColumnType("nvarchar(100)");
            builder.Property(r => r.CourierPhone).HasColumnType("nvarchar(20)");
            builder.Property(r => r.AdminNote).HasColumnType("nvarchar(500)");

            builder.HasOne(r => r.Order)
                .WithMany(o => o.ReturnRequests)
                .OnDelete(DeleteBehavior.NoAction)
                .HasForeignKey(r => r.OrderId);

            builder.HasOne(r => r.Customer)
                .WithMany()
                .OnDelete(DeleteBehavior.NoAction)
                .HasForeignKey(r => r.CustomerId);

            builder.HasIndex(r => r.OrderId);

            builder.HasQueryFilter(r => !r.IsDeleted);
        }
    }
}
