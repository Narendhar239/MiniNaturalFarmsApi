using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniNaturalFarms.Domain.Common;
using MiniNaturalFarms.Domain.Entities;
using MiniNaturalFarms.Infrastructure.Identity;

namespace MiniNaturalFarms.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>(e =>
        {
            e.Property(x => x.NameEn).HasMaxLength(150).IsRequired();
            e.Property(x => x.NameTe).HasMaxLength(150).IsRequired();
        });

        builder.Entity<Product>(e =>
        {
            e.Property(x => x.NameEn).HasMaxLength(250).IsRequired();
            e.Property(x => x.NameTe).HasMaxLength(250).IsRequired();
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.Property(x => x.DiscountPrice).HasPrecision(18, 2);
            e.HasOne(x => x.Category).WithMany(c => c.Products).HasForeignKey(x => x.CategoryId);
            e.HasIndex(x => x.CategoryId);
        });

        builder.Entity<ProductImage>(e =>
            e.HasOne(x => x.Product).WithMany(p => p.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade));

        builder.Entity<CartItem>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();
        });

        builder.Entity<WishlistItem>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();
        });

        builder.Entity<Order>(e =>
        {
            e.Property(x => x.SubTotal).HasPrecision(18, 2);
            e.Property(x => x.ShippingCharge).HasPrecision(18, 2);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.HasIndex(x => x.OrderNumber).IsUnique();
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.RazorpayOrderId);
            e.Property(x => x.OrderNumber).HasMaxLength(40).IsRequired();
            e.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            e.Property(x => x.RazorpayOrderId).HasMaxLength(100);
            e.Property(x => x.RazorpayPaymentId).HasMaxLength(100);
            e.Property(x => x.RazorpayRefundId).HasMaxLength(100);
        });

        builder.Entity<OrderItem>(e =>
        {
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.HasOne(x => x.Order).WithMany(o => o.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.ProductName).HasMaxLength(250).IsRequired();
        });
        builder.Entity<Address>(e =>
        {
            e.HasIndex(x => x.UserId);
            e.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(15).IsRequired();
            e.Property(x => x.Line1).HasMaxLength(250).IsRequired();
            e.Property(x => x.Line2).HasMaxLength(250);
            e.Property(x => x.City).HasMaxLength(100).IsRequired();
            e.Property(x => x.State).HasMaxLength(100).IsRequired();
            e.Property(x => x.Pincode).HasMaxLength(10).IsRequired();
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
