using Microsoft.EntityFrameworkCore;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Auth;

namespace MyApi.Shared.Data;

public static class Seed
{
    public const string Password = "Password123";
    private const string MarkerEmail = "techstore@myapi.dev";
    private const string AdminEmail = "admin@myapi.dev";

    public static async Task RunAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await EnsureAdminAsync(dbContext, passwordHasher, ct);

        if (await dbContext.UserAccounts.AnyAsync(x => x.Email == MarkerEmail, ct))
        {
            return;
        }

        var techOwner = new UserAccount("techstore_owner", MarkerEmail, passwordHasher.Hash(Password));
        var homeOwner = new UserAccount("homegoods_owner", "homegoods@myapi.dev", passwordHasher.Hash(Password));
        var buyer = new UserAccount("demo_buyer", "buyer@myapi.dev", passwordHasher.Hash(Password));
        dbContext.UserAccounts.AddRange(techOwner, homeOwner, buyer);

        string[] categoryNames = ["Electronics", "Home & Kitchen", "Clothing", "Books"];
        var categories = await dbContext.Categories
            .Where(x => categoryNames.Contains(x.Name))
            .ToDictionaryAsync(x => x.Name, ct);

        foreach (var name in categoryNames.Where(x => !categories.ContainsKey(x)))
        {
            var category = new Category(name);
            dbContext.Categories.Add(category);
            categories[name] = category;
        }

        var techStore = new Seller("TechStore", techOwner.Id);
        techStore.ChangeRating(4.8m);

        var homeGoods = new Seller("Home Goods", homeOwner.Id);
        homeGoods.ChangeRating(4.5m);

        dbContext.Sellers.AddRange(techStore, homeGoods);

        var products = new[]
        {
            CreateProduct("Smartphone X12", "TS-PHONE-X12", 49990m, 25, categories["Electronics"], techStore,
                "6.5\" OLED display, 256 GB storage, triple camera."),
            CreateProduct("Wireless Earbuds Pro", "TS-EARBUDS-PRO", 7990m, 120, categories["Electronics"], techStore,
                "Active noise cancellation, 30 hours of battery life."),
            CreateProduct("Laptop Air 14", "TS-LAPTOP-AIR14", 89990m, 8, categories["Electronics"], techStore,
                "14\" display, 16 GB RAM, 512 GB SSD."),
            CreateProduct("USB-C Charger 65W", "TS-CHARGER-65W", 2490m, 300, categories["Electronics"], techStore,
                null),
            CreateProduct("C# in Depth", "TS-BOOK-CSHARP", 3200m, 15, categories["Books"], techStore,
                "Fourth edition."),
            CreateProduct("Ceramic Mug Set (4 pcs)", "HG-MUG-SET4", 1290m, 60, categories["Home & Kitchen"], homeGoods,
                "Dishwasher and microwave safe."),
            CreateProduct("Cast Iron Skillet 28 cm", "HG-SKILLET-28", 3490m, 40, categories["Home & Kitchen"], homeGoods,
                "Pre-seasoned, suitable for all hob types."),
            CreateProduct("Linen Shirt", "HG-SHIRT-LINEN", 2990m, 0, categories["Clothing"], homeGoods,
                "100% linen, out of stock."),
        };

        var discontinued = CreateProduct("Smartphone X10", "TS-PHONE-X10", 29990m, 0, categories["Electronics"], techStore,
            "Discontinued model.");
        discontinued.Deactivate();

        dbContext.Products.AddRange(products);
        dbContext.Products.Add(discontinued);

        await dbContext.SaveChangesAsync(ct);
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

    private static Product CreateProduct(
        string name, string sku, decimal price, int stock, Category category, Seller seller, string? description)
    {
        var product = new Product(name, sku, price, stock, category.Id, seller.Id);
        product.ChangeDescription(description);

        return product;
    }
}
