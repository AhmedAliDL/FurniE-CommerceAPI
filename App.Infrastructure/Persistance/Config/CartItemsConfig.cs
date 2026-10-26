using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistance.Config
{
    public class CartItemsConfig : IEntityTypeConfiguration<CartItem>
    {
        public void Configure(EntityTypeBuilder<CartItem> builder)
        {
            builder.HasKey(ci => ci.Id);
            builder.Property(ci => ci.Id).ValueGeneratedOnAdd();

            builder.Property(ci => ci.ItemQuantity).HasColumnType("int");


            builder.HasOne(ci => ci.ShoppingCart)
                 .WithMany(sc => sc.ProductsCartItems)
                 .OnDelete(DeleteBehavior.NoAction)
                 .HasForeignKey(ci => ci.ShoppingCartId);
            builder.HasOne(ci => ci.Product)
                .WithMany(p => p.CartItems)
                .OnDelete(DeleteBehavior.Cascade)
                .HasForeignKey(ci => ci.ProductId);

            // Soft-deleted rows stay in the table, so the uniqueness must only cover live rows;
            // otherwise re-adding a removed product collides with the tombstone.
            builder.HasIndex(ci => new { ci.ShoppingCartId, ci.ProductId }).IsUnique().HasFilter("[IsDeleted] = 0");

            builder.HasQueryFilter(ci => !ci.IsDeleted);
        }
    }
}
