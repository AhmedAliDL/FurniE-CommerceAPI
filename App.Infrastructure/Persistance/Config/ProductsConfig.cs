using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistance.Config
{
    public class ProductsConfig : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasKey(p => p.ProductId);
            builder.Property(p => p.ProductId).ValueGeneratedOnAdd();

            builder.Property(p => p.ProductName).HasColumnType("nvarchar(50)");
            builder.Property(p => p.Description).HasColumnType("text");
            builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
            builder.Property(p => p.StockQuantity).HasColumnType("int");
            builder.Property(p => p.ImageUrl).HasColumnType("nvarchar(1000)");
            builder.Property(p => p.CreatedAt).HasColumnType("datetime");
            builder.Property(p => p.IsActive).HasColumnType("bit");

            builder.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .OnDelete(DeleteBehavior.Cascade)
                .HasForeignKey(p => p.CategoryId);

            builder.HasQueryFilter(p => !p.IsDeleted);
        }
    }
}
