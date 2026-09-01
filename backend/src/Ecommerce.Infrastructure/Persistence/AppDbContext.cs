using Ecommerce.Domain.Catalog;
using Ecommerce.Domain.Inventory;
using Ecommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<InventoryRecord> Inventory => Set<InventoryRecord>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.TokenHash).IsRequired().HasMaxLength(256);
            entity.HasIndex(rt => rt.TokenHash).IsUnique();
            entity.HasIndex(rt => rt.UserId);
        });

        builder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.Property(c => c.Name).IsRequired().HasMaxLength(150);
            entity.Property(c => c.Slug).IsRequired().HasMaxLength(220);
            entity.Property(c => c.Description).HasMaxLength(1000);
            entity.HasIndex(c => c.Slug).IsUnique();
        });

        builder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Slug).IsRequired().HasMaxLength(220);
            entity.Property(p => p.Description).HasMaxLength(4000);
            entity.Property(p => p.Price).HasPrecision(10, 2);
            entity.HasIndex(p => p.Slug).IsUnique();
            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProductVariant>(entity =>
        {
            entity.ToTable("ProductVariants");
            entity.Property(v => v.Color).IsRequired().HasMaxLength(100);
            entity.Property(v => v.Size).IsRequired().HasMaxLength(50);
            entity.Property(v => v.Sku).IsRequired().HasMaxLength(64);
            entity.Property(v => v.PriceOverride).HasPrecision(10, 2);
            entity.HasIndex(v => v.Sku).IsUnique();
            entity.HasOne(v => v.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("ProductImages");
            entity.Property(i => i.Url).IsRequired().HasMaxLength(2000);
            entity.Property(i => i.AltText).HasMaxLength(300);
            entity.HasOne(i => i.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InventoryRecord>(entity =>
        {
            entity.ToTable("Inventory");
            entity.HasIndex(i => i.ProductVariantId).IsUnique();
            entity.HasOne(i => i.ProductVariant)
                .WithOne(v => v.Inventory)
                .HasForeignKey<InventoryRecord>(i => i.ProductVariantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InventoryTransaction>(entity =>
        {
            entity.ToTable("InventoryTransactions");
            entity.Property(t => t.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(t => t.Reason).HasMaxLength(500);
            entity.HasIndex(t => t.ProductVariantId);
            entity.HasOne(t => t.ProductVariant)
                .WithMany()
                .HasForeignKey(t => t.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
