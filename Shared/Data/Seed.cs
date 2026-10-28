using Microsoft.EntityFrameworkCore;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Auth;
using MyApi.Services.Interfaces.Images;

namespace MyApi.Shared.Data;

public static class Seed
{
    public const string Password = "Password123";

    private const string AdminEmail = "admin@myapi.dev";

    private static readonly DemoUser[] Users =
    [
        new("techstore_owner", "techstore@myapi.dev", "TechStore", 4.8m),
        new("homegoods_owner", "homegoods@myapi.dev", "Home Goods", 4.5m),
        new("demo_buyer", "buyer@myapi.dev", null, null)
    ];

    private static readonly string[] CategoryNames = ["Electronics", "Home & Kitchen", "Clothing", "Books"];

    private static readonly DemoProduct[] Products =
    [
        new("TS-PHONE-X12", "Smartphone X12", 499.99m, 49990m, 25, "Electronics", "TechStore",
            "6.5\" OLED display, 256 GB storage, triple camera.", IsActive: true),
        new("TS-EARBUDS-PRO", "Wireless Earbuds Pro", 79.99m, 7990m, 120, "Electronics", "TechStore",
            "Active noise cancellation, 30 hours of battery life.", IsActive: true),
        new("TS-LAPTOP-AIR14", "Laptop Air 14", 899.99m, 89990m, 8, "Electronics", "TechStore",
            "14\" display, 16 GB RAM, 512 GB SSD.", IsActive: true),
        new("TS-CHARGER-65W", "USB-C Charger 65W", 24.99m, 2490m, 300, "Electronics", "TechStore",
            null, IsActive: true),
        new("TS-BOOK-CSHARP", "C# in Depth", 39.99m, 3200m, 15, "Books", "TechStore",
            "Fourth edition.", IsActive: true),
        new("TS-PHONE-X10", "Smartphone X10", 299.99m, 29990m, 0, "Electronics", "TechStore",
            "Discontinued model.", IsActive: false),
        new("HG-MUG-SET4", "Ceramic Mug Set (4 pcs)", 12.99m, 1290m, 60, "Home & Kitchen", "Home Goods",
            "Dishwasher and microwave safe.", IsActive: true),
        new("HG-SKILLET-28", "Cast Iron Skillet 28 cm", 34.99m, 3490m, 40, "Home & Kitchen", "Home Goods",
            "Pre-seasoned, suitable for all hob types.", IsActive: true),
        new("HG-SHIRT-LINEN", "Linen Shirt", 29.99m, 2990m, 0, "Clothing", "Home Goods",
            "100% linen, out of stock.", IsActive: true)
    ];

    public static async Task RunAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var dbContext = provider.GetRequiredService<AppDbContext>();
        var passwordHasher = provider.GetRequiredService<IPasswordHasher>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(Seed));

        await EnsureAdminAsync(dbContext, passwordHasher, ct);
        await EnsureCatalogAsync(dbContext, passwordHasher, ct);
        await ReplaceLegacyPricesAsync(dbContext, ct);

        try
        {
            await EnsureImagesAsync(dbContext, provider.GetRequiredService<IImageStorage>(), ct);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception,
                "Demo images were not created. Start MinIO with 'docker compose up -d' and restart the API.");
        }
    }

    private static async Task EnsureAdminAsync(
        AppDbContext dbContext, IPasswordHasher passwordHasher, CancellationToken ct)
    {
        if (await dbContext.UserAccounts.AnyAsync(x => x.Email == AdminEmail, ct))
        {
            return;
        }

        var admin = new UserAccount("platform_admin", AdminEmail, passwordHasher.Hash(Password));
        admin.ChangeRole(UserRole.Admin);

        dbContext.UserAccounts.Add(admin);
        await dbContext.SaveChangesAsync(ct);
    }

    private static async Task EnsureCatalogAsync(
        AppDbContext dbContext, IPasswordHasher passwordHasher, CancellationToken ct)
    {
        if (await dbContext.UserAccounts.AnyAsync(x => x.Email == Users[0].Email, ct))
        {
            return;
        }

        var accounts = Users.ToDictionary(
            x => x.Email,
            x => new UserAccount(x.Username, x.Email, passwordHasher.Hash(Password)));
        dbContext.UserAccounts.AddRange(accounts.Values);

        var categories = await dbContext.Categories
            .Where(x => CategoryNames.Contains(x.Name))
            .ToDictionaryAsync(x => x.Name, ct);

        foreach (var name in CategoryNames.Where(x => !categories.ContainsKey(x)))
        {
            var category = new Category(name);
            dbContext.Categories.Add(category);
            categories[name] = category;
        }

        var sellers = new Dictionary<string, Seller>();
        foreach (var user in Users.Where(x => x.ShopName is not null))
        {
            var seller = new Seller(user.ShopName!, accounts[user.Email].Id);
            seller.ChangeRating(user.ShopRating!.Value);

            dbContext.Sellers.Add(seller);
            sellers[user.ShopName!] = seller;
        }

        foreach (var demo in Products)
        {
            var product = new Product(
                demo.Name, demo.Sku, demo.Price, demo.Stock, categories[demo.Category].Id, sellers[demo.Shop].Id);
            product.ChangeDescription(demo.Description);

            if (!demo.IsActive)
            {
                product.Deactivate();
            }

            dbContext.Products.Add(product);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private static async Task ReplaceLegacyPricesAsync(AppDbContext dbContext, CancellationToken ct)
    {
        var skus = Products.Select(x => x.Sku).ToList();
        var products = await dbContext.Products.Where(x => skus.Contains(x.Sku)).ToListAsync(ct);

        foreach (var product in products)
        {
            var demo = Products.Single(x => x.Sku == product.Sku);

            if (product.Price == demo.LegacyPrice)
            {
                product.ChangePrice(demo.Price);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private static async Task EnsureImagesAsync(
        AppDbContext dbContext, IImageStorage imageStorage, CancellationToken ct)
    {
        var skus = Products.Select(x => x.Sku).ToList();
        var products = await dbContext.Products
            .Include(x => x.Images)
            .Where(x => skus.Contains(x.Sku) && x.Images.Count == 0)
            .ToListAsync(ct);

        foreach (var product in products)
        {
            var demo = Products.Single(x => x.Sku == product.Sku);

            for (var variant = 0; variant < DemoImages.ProductVariants; variant++)
            {
                var storageKey = await SaveAsync(
                    imageStorage,
                    DemoImages.Product(demo.Name, demo.Category, variant),
                    ImageKind.ProductPhoto,
                    $"products/{product.Id}",
                    ct);

                dbContext.ProductImages.Add(product.AddImage(storageKey));
            }

            await dbContext.SaveChangesAsync(ct);
        }

        var shopNames = Users.Where(x => x.ShopName is not null).Select(x => x.ShopName!).ToList();
        var sellers = await dbContext.Sellers
            .Where(x => shopNames.Contains(x.Name) && x.LogoKey == null)
            .ToListAsync(ct);

        foreach (var seller in sellers)
        {
            seller.ChangeLogo(await SaveAsync(
                imageStorage, DemoImages.Logo(seller.Name), ImageKind.SellerLogo, $"sellers/{seller.Id}", ct));
        }

        var emails = Users.Select(x => x.Email).ToList();
        var users = await dbContext.UserAccounts
            .Where(x => emails.Contains(x.Email) && x.AvatarKey == null)
            .ToListAsync(ct);

        foreach (var user in users)
        {
            user.ChangeAvatar(await SaveAsync(
                imageStorage, DemoImages.Avatar(user.Username), ImageKind.UserAvatar, $"users/{user.Id}", ct));
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private static async Task<string> SaveAsync(
        IImageStorage imageStorage, byte[] png, ImageKind kind, string keyPrefix, CancellationToken ct)
    {
        await using var content = new MemoryStream(png, writable: false);

        return await imageStorage.SaveAsync(new ImageUpload(content, "image/png", png.Length), kind, keyPrefix, ct);
    }

    private sealed record DemoUser(string Username, string Email, string? ShopName, decimal? ShopRating);

    private sealed record DemoProduct(
        string Sku,
        string Name,
        decimal Price,
        decimal LegacyPrice,
        int Stock,
        string Category,
        string Shop,
        string? Description,
        bool IsActive);
}
