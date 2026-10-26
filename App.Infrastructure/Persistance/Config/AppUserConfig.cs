using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistance.Config
{
    public class AppUserConfig : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            builder.Property(u => u.FName).HasMaxLength(20);
            builder.Property(u => u.LName).HasMaxLength(20);
            builder.Property(u => u.Country).HasMaxLength(120);
            builder.Property(u => u.City).HasMaxLength(80);
            builder.Property(u => u.Address).HasMaxLength(100);
        }
    }
}
