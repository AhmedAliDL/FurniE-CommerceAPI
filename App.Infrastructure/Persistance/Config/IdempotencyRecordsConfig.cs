using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistance.Config
{
    public class IdempotencyRecordsConfig : IEntityTypeConfiguration<IdempotencyRecord>
    {
        public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).ValueGeneratedOnAdd();

            builder.Property(r => r.IdempotencyKey).IsRequired().HasMaxLength(100);
            builder.Property(r => r.ResponsePayload).HasColumnType("nvarchar(max)");
            builder.Property(r => r.CreatedAt).HasColumnType("datetime2");
            builder.Property(r => r.UpdatedAt).HasColumnType("datetime2");

            builder.HasIndex(r => new { r.UserId, r.IdempotencyKey }).IsUnique();

            // Optional so the record stays queryable for replay after its order is soft-deleted; Cascade kept explicitly since convention would downgrade it.
            builder.HasOne(r => r.Order)
                .WithMany(o => o.IdempotencyRecords)
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
