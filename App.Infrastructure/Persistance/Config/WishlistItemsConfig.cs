using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistance.Config
{
    public class WishlistItemsConfig : IEntityTypeConfiguration<WishlistItem>
    {
        public void Configure(EntityTypeBuilder<WishlistItem> builder)
        {
            builder.HasKey(w => w.Id);
            builder.Property(w => w.Id).ValueGeneratedOnAdd();
            builder.Property(w => w.ProductOriginalPrice).HasColumnType("decimal(18,2)");

            builder.HasIndex(w => new { w.UserId, w.ProductId }).IsUnique().HasFilter("[IsDeleted] = 0");

            builder.HasOne(w => w.User)
                .WithMany(u => u.WishlistItems)
                .OnDelete(DeleteBehavior.NoAction)
                .HasForeignKey(w => w.UserId);

            builder.HasOne(w => w.Product)
                .WithMany()
                .OnDelete(DeleteBehavior.Cascade)
                .HasForeignKey(w => w.ProductId);

            builder.HasQueryFilter(w => !w.IsDeleted);
        }
    }
}
