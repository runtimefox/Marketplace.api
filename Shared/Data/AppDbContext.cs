using Microsoft.EntityFrameworkCore;
using MyApi.Models.Entities;

namespace MyApi.Shared.Data;

public class AppDbContext: DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<UserAccount> UserAccounts { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Seller> Sellers { get; set; }
    public DbSet<SellerMember> SellerMembers { get; set; }

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
    }
}
