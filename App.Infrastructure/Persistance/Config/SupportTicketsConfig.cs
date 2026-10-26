using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistance.Config
{
    public class SupportTicketsConfig : IEntityTypeConfiguration<SupportTicket>
    {
        public void Configure(EntityTypeBuilder<SupportTicket> builder)
        {
            builder.HasKey(t => t.TicketId);
            builder.Property(t => t.TicketId).ValueGeneratedOnAdd();

            builder.Property(t => t.Subject).IsRequired().HasMaxLength(200);
            builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(30);
            builder.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20);

            builder.HasIndex(t => t.CustomerId);

            builder.HasOne(t => t.Customer)
                .WithMany()
                .OnDelete(DeleteBehavior.NoAction)
                .HasForeignKey(t => t.CustomerId);

            builder.HasQueryFilter(t => !t.IsDeleted);
        }
    }
}
