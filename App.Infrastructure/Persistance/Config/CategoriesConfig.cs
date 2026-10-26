using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistance.Config
{
    public class CategoriesConfig : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasKey(c => c.CategoryId);
            builder.Property(c => c.CategoryId).ValueGeneratedOnAdd();

            builder.Property(c => c.CategoryName).HasColumnType("nvarchar(50)");

            builder.HasIndex(c => c.CategoryName)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasQueryFilter(c => !c.IsDeleted);
        }
    }
}
