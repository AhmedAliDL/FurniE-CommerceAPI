using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistance.Config
{
    public class ShoppingCartConfig : IEntityTypeConfiguration<ShoppingCart>
    {
        public void Configure(EntityTypeBuilder<ShoppingCart> builder)
        {
            builder.HasKey(sc => sc.Id);
            builder.Property(sc => sc.Id).ValueGeneratedOnAdd();

            builder.Property(sc => sc.CreatedAt).HasColumnType("datetime");

            builder.HasOne(sc => sc.Customer)
                .WithOne(u => u.ShoppingCart)
                .OnDelete(DeleteBehavior.NoAction)
                .HasForeignKey<ShoppingCart>(sc => sc.UserId);

            builder.HasQueryFilter(sc => !sc.IsDeleted);
        }
    }
}
