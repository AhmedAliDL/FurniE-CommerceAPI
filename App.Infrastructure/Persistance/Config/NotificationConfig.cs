using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistance.Config
{
    public class NotificationConfig : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasKey(n => n.Id);
            builder.Property(n => n.Id).ValueGeneratedOnAdd();

            builder.HasOne(n => n.Receiver)
                .WithMany(u => u.RecieverNotifications)
                .HasForeignKey(n => n.ReceiverId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(n => n.Sender)
                .WithMany(u => u.SenderNotifications)
                .HasForeignKey(n => n.SenderId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(n => !n.IsDeleted);
        }
    }
}
