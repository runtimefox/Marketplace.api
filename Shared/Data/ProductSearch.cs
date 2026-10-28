using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MyApi.Models.Entities;
using NpgsqlTypes;

namespace MyApi.Shared.Data;

public static partial class ProductSearch
{
    public const string VectorColumn = "SearchVector";
    public const string TextSearchConfig = "english";
    public const int MaxLength = 200;
    public const int MaxTerms = 10;

    public const string VectorSql =
        $"setweight(to_tsvector('{TextSearchConfig}', \"Name\"), 'A') || " +
        $"setweight(to_tsvector('{TextSearchConfig}', coalesce(\"Description\", '')), 'B')";

    public static IQueryable<Product> Search(this IQueryable<Product> products, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return products;
        }

        var query = ToTsQuery(search);
        var sku = search.Trim().ToUpperInvariant();

        return products
            .Where(x => x.Sku == sku
                        || EF.Property<NpgsqlTsVector>(x, VectorColumn)
                            .Matches(EF.Functions.ToTsQuery(TextSearchConfig, query)))
            .OrderByDescending(x => x.Sku == sku)
            .ThenByDescending(x => EF.Property<NpgsqlTsVector>(x, VectorColumn)
                .Rank(EF.Functions.ToTsQuery(TextSearchConfig, query)))
            .ThenBy(x => x.Id);
    }

    public static string ToTsQuery(string search)
    {
        if (search.Length > MaxLength)
        {
            throw new ArgumentException($"Search must not exceed {MaxLength} characters.", nameof(search));
        }

        var terms = Term().Matches(search)
            .Select(x => x.Value.ToLowerInvariant())
            .Distinct()
            .Take(MaxTerms)
            .Select(x => $"{x}:*");

        return string.Join(" & ", terms);
    }

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex Term();
}
