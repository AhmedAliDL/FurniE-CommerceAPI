using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistance.Config
{
    public class ProductImagesConfig : IEntityTypeConfiguration<ProductImage>
    {
        public void Configure(EntityTypeBuilder<ProductImage> builder)
        {
            builder.HasKey(i => i.Id);
            builder.Property(i => i.Id).ValueGeneratedOnAdd();
            builder.Property(i => i.ImageUrl).IsRequired().HasColumnType("nvarchar(1000)");

            builder.HasOne(i => i.Product)
                .WithMany(p => p.ProductImages)
                .OnDelete(DeleteBehavior.Cascade)
                .HasForeignKey(i => i.ProductId);

            builder.HasIndex(i => i.ProductId);
            // Only one primary image per product among live rows.
            builder.HasIndex(i => new { i.ProductId, i.IsPrimary })
                .IsUnique()
                .HasFilter("[IsPrimary] = 1 AND [IsDeleted] = 0");

            builder.HasQueryFilter(i => !i.IsDeleted);
        }
    }
}
