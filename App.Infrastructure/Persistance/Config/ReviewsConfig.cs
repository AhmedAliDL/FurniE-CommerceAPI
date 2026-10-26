using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistance.Config
{
    public class ReviewsConfig : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> builder)
        {
            builder.HasKey(r => r.ReviewId);
            builder.Property(r => r.ReviewId).ValueGeneratedOnAdd();
            builder.Property(r => r.Rating).HasColumnType("int");
            builder.Property(r => r.Comment).HasColumnType("text");
            builder.Property(r => r.CreatedAt).HasColumnType("datetime");

            builder.HasIndex(r => new { r.UserId, r.ProductId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne(r => r.Customer)
                .WithMany(u => u.Reviews)
                .OnDelete(DeleteBehavior.NoAction)
                .HasForeignKey(r => r.UserId);

            builder.HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .OnDelete(DeleteBehavior.Cascade)
                .HasForeignKey(r => r.ProductId);

            builder.HasQueryFilter(r => !r.IsDeleted);
        }
    }
}
