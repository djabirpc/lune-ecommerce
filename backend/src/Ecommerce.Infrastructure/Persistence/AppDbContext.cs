using Ecommerce.Domain.Catalog;
using Ecommerce.Domain.Inventory;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Promotions;
using Ecommerce.Domain.Shipping;
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

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<OrderCallAttempt> OrderCallAttempts => Set<OrderCallAttempt>();
    public DbSet<OrderPromotion> OrderPromotions => Set<OrderPromotion>();

    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionProduct> PromotionProducts => Set<PromotionProduct>();
    public DbSet<PromotionCategory> PromotionCategories => Set<PromotionCategory>();

    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentTrackingEvent> ShipmentTrackingEvents => Set<ShipmentTrackingEvent>();

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

        builder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.Property(o => o.OrderNumber).IsRequired().HasMaxLength(20);
            entity.Property(o => o.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(o => o.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(o => o.LastName).IsRequired().HasMaxLength(100);
            entity.Property(o => o.Phone).IsRequired().HasMaxLength(20);
            entity.Property(o => o.Wilaya).IsRequired().HasMaxLength(100);
            entity.Property(o => o.Commune).IsRequired().HasMaxLength(100);
            entity.Property(o => o.Address).IsRequired().HasMaxLength(500);
            entity.Property(o => o.DeliveryType).HasConversion<string>().HasMaxLength(20);
            entity.Property(o => o.Notes).HasMaxLength(1000);
            entity.Property(o => o.PaymentMethod).IsRequired().HasMaxLength(20);
            entity.Property(o => o.PaymentStatus).HasConversion<string>().HasMaxLength(20);
            entity.Property(o => o.Subtotal).HasPrecision(10, 2);
            entity.Property(o => o.ShippingCost).HasPrecision(10, 2);
            entity.Property(o => o.DiscountTotal).HasPrecision(10, 2);
            entity.Property(o => o.Total).HasPrecision(10, 2);
            entity.HasIndex(o => o.OrderNumber).IsUnique();
            entity.HasIndex(o => o.Phone);
        });

        builder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(i => i.Color).IsRequired().HasMaxLength(100);
            entity.Property(i => i.Size).IsRequired().HasMaxLength(50);
            entity.Property(i => i.Sku).IsRequired().HasMaxLength(64);
            entity.Property(i => i.UnitPrice).HasPrecision(10, 2);
            entity.Property(i => i.LineTotal).HasPrecision(10, 2);
            entity.HasOne(i => i.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OrderStatusHistory>(entity =>
        {
            entity.ToTable("OrderStatusHistories");
            entity.Property(h => h.OldStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(h => h.NewStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(h => h.Reason).HasMaxLength(500);
            entity.HasOne(h => h.Order)
                .WithMany(o => o.StatusHistory)
                .HasForeignKey(h => h.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OrderCallAttempt>(entity =>
        {
            entity.ToTable("OrderCallAttempts");
            entity.Property(a => a.Result).HasConversion<string>().HasMaxLength(30);
            entity.Property(a => a.Notes).HasMaxLength(1000);
            entity.HasIndex(a => a.OrderId);
            entity.HasOne(a => a.Order)
                .WithMany(o => o.CallAttempts)
                .HasForeignKey(a => a.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OrderPromotion>(entity =>
        {
            entity.ToTable("OrderPromotions");
            entity.Property(p => p.PromotionName).IsRequired().HasMaxLength(200);
            entity.Property(p => p.DiscountAmount).HasPrecision(10, 2);
            entity.HasIndex(p => p.OrderId);
            entity.HasOne(p => p.Order)
                .WithMany(o => o.AppliedPromotions)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Promotion>(entity =>
        {
            entity.ToTable("Promotions");
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Description).HasMaxLength(2000);
            entity.Property(p => p.Type).HasConversion<string>().HasMaxLength(30);
            entity.Property(p => p.PercentageValue).HasPrecision(5, 2);
            entity.Property(p => p.FixedAmountValue).HasPrecision(10, 2);
            entity.Property(p => p.CouponCode).HasMaxLength(50);
            entity.HasIndex(p => p.CouponCode).IsUnique().HasFilter("\"CouponCode\" IS NOT NULL");
            entity.HasIndex(p => new { p.IsActive, p.StartsAtUtc, p.EndsAtUtc });
        });

        builder.Entity<PromotionProduct>(entity =>
        {
            entity.ToTable("PromotionProducts");
            entity.HasKey(pp => new { pp.PromotionId, pp.ProductId });
            entity.HasOne(pp => pp.Promotion)
                .WithMany(p => p.Products)
                .HasForeignKey(pp => pp.PromotionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Product>()
                .WithMany()
                .HasForeignKey(pp => pp.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PromotionCategory>(entity =>
        {
            entity.ToTable("PromotionCategories");
            entity.HasKey(pc => new { pc.PromotionId, pc.CategoryId });
            entity.HasOne(pc => pc.Promotion)
                .WithMany(p => p.Categories)
                .HasForeignKey(pc => pc.PromotionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Category>()
                .WithMany()
                .HasForeignKey(pc => pc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Shipment>(entity =>
        {
            entity.ToTable("Shipments");
            entity.Property(s => s.Carrier).HasConversion<string>().HasMaxLength(20);
            entity.Property(s => s.ProviderShipmentId).IsRequired().HasMaxLength(100);
            entity.Property(s => s.TrackingNumber).HasMaxLength(100);
            entity.Property(s => s.ProviderStatus).IsRequired().HasMaxLength(50);
            entity.Property(s => s.NormalizedStatus).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(s => s.OrderId).IsUnique();
            entity.HasOne(s => s.Order)
                .WithOne(o => o.Shipment)
                .HasForeignKey<Shipment>(s => s.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ShipmentTrackingEvent>(entity =>
        {
            entity.ToTable("ShipmentTrackingEvents");
            entity.Property(e => e.ProviderStatus).IsRequired().HasMaxLength(50);
            entity.Property(e => e.NormalizedStatus).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.ShipmentId);
            entity.HasOne(e => e.Shipment)
                .WithMany(s => s.TrackingEvents)
                .HasForeignKey(e => e.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
