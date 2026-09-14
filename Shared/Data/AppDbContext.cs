using Microsoft.EntityFrameworkCore;
using MyApi.Models.Entities;

namespace MyApi.Shared.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> UserAccounts { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Seller> Sellers { get; set; }
    public DbSet<SellerMember> SellerMembers { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Review> Reviews { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserAccount>()
            .HasIndex(x => x.Username)
            .IsUnique();
        modelBuilder.Entity<UserAccount>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<UserAccount>()
            .Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(x => x.TokenHash)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Category>()
            .HasIndex(x => x.Name)
            .IsUnique();

        modelBuilder.Entity<Product>()
            .HasIndex(x => x.Sku)
            .IsUnique();

        modelBuilder.Entity<Product>()
            .HasIndex(x => new { x.CategoryId, x.IsActive });

        modelBuilder.Entity<Product>()
            .Property(x => x.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Product>()
            .HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .HasOne(x => x.Seller)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Seller>()
            .Property(x => x.Rating)
            .HasPrecision(3, 2);

        modelBuilder.Entity<Seller>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_Sellers_Rating",
                $"\"Rating\" >= {Seller.MinRating} AND \"Rating\" <= {Seller.MaxRating}"));

        modelBuilder.Entity<SellerMember>()
            .HasIndex(x => new { x.SellerId, x.UserAccountId })
            .IsUnique();

        modelBuilder.Entity<SellerMember>()
            .Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        modelBuilder.Entity<SellerMember>()
            .HasOne(x => x.Seller)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SellerMember>()
            .HasOne(x => x.UserAccount)
            .WithMany()
            .HasForeignKey(x => x.UserAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .Property(x => x.Version)
            .IsRowVersion();

        modelBuilder.Entity<CartItem>()
            .HasIndex(x => new { x.UserAccountId, x.ProductId })
            .IsUnique();

        modelBuilder.Entity<CartItem>()
            .HasOne(x => x.UserAccount)
            .WithMany()
            .HasForeignKey(x => x.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CartItem>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        modelBuilder.Entity<Order>()
            .Property(x => x.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .HasIndex(x => new { x.BuyerId, x.CreatedAt });

        modelBuilder.Entity<Order>()
            .HasIndex(x => new { x.SellerId, x.Status });

        modelBuilder.Entity<Order>()
            .HasOne(x => x.Buyer)
            .WithMany()
            .HasForeignKey(x => x.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(x => x.Seller)
            .WithMany()
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .OwnsOne(x => x.DeliveryAddress);

        modelBuilder.Entity<Order>()
            .Navigation(x => x.DeliveryAddress)
            .IsRequired();

        modelBuilder.Entity<Order>()
            .HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .Property(x => x.UnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderItem>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasIndex(x => new { x.ProductId, x.UserAccountId })
            .IsUnique();

        modelBuilder.Entity<Review>()
            .HasOne(x => x.Product)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(x => x.UserAccount)
            .WithMany()
            .HasForeignKey(x => x.UserAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_Reviews_Rating",
                $"\"Rating\" >= {Review.MinRating} AND \"Rating\" <= {Review.MaxRating}"));
    }
}
