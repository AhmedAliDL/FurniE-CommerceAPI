using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistance.Config
{
    public class IntegrationOutboxConfig : IEntityTypeConfiguration<IntegrationOutboxMessage>
    {
        public void Configure(EntityTypeBuilder<IntegrationOutboxMessage> builder)
        {
            builder.ToTable("IntegrationOutbox");
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Id).ValueGeneratedOnAdd();

            builder.Property(m => m.EventType).IsRequired().HasMaxLength(100);
            builder.Property(m => m.Payload).IsRequired().HasColumnType("nvarchar(max)");
            builder.Property(m => m.LastError).HasColumnType("nvarchar(2000)");
            builder.Property(m => m.OccurredAt).HasColumnType("datetime2");
            builder.Property(m => m.ProcessedAt).HasColumnType("datetime2");
            builder.Property(m => m.CreatedAt).HasColumnType("datetime2");

            builder.HasIndex(m => new { m.ProcessedAt, m.OccurredAt });
        }
    }
}
