using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistance.Config
{
    public class SupportConversationsConfig : IEntityTypeConfiguration<SupportConversation>
    {
        public void Configure(EntityTypeBuilder<SupportConversation> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).ValueGeneratedOnAdd();

            builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);

            builder.HasIndex(c => c.SupportTicketId).IsUnique().HasFilter("[IsDeleted] = 0");

            builder.HasOne(c => c.SupportTicket)
                .WithMany()
                .HasForeignKey(c => c.SupportTicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(c => !c.IsDeleted);
        }
    }
}
