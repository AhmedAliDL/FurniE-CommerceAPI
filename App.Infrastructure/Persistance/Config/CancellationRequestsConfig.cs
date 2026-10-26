using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistance.Config
{
    public class CancellationRequestsConfig : IEntityTypeConfiguration<CancellationRequest>
    {
        public void Configure(EntityTypeBuilder<CancellationRequest> builder)
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).ValueGeneratedOnAdd();
            builder.Property(r => r.Reason).HasColumnType("nvarchar(500)");
            builder.Property(r => r.PreviousOrderStatus).HasColumnType("nvarchar(30)");
            builder.Property(r => r.Status).HasColumnType("nvarchar(30)");
            builder.Property(r => r.AdminNote).HasColumnType("nvarchar(500)");

            builder.HasOne(r => r.Order)
                .WithMany(o => o.CancellationRequests)
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
