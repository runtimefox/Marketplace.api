using MyApi.Shared.Data;

namespace MyApi.Tests.Unit;

public class ProductSearchQueryTests
{
    [Theory]
    [InlineData("Wireless", "wireless:*")]
    [InlineData("  wirel   HEAD ", "wirel:* & head:*")]
    [InlineData("USB-C charger 65W", "usb:* & c:* & charger:* & 65w:*")]
    [InlineData("café Müller", "café:* & müller:*")]
    public void ToTsQuery_TurnsEveryWordIntoPrefix(string search, string expected) =>
        Assert.Equal(expected, ProductSearch.ToTsQuery(search));

    [Fact]
    public void ToTsQuery_StripsQuerySyntax()
    {
        Assert.Equal("phone:* & case:*", ProductSearch.ToTsQuery("phone' & !case | (:*)"));
        Assert.Equal(string.Empty, ProductSearch.ToTsQuery("&|!():*'"));
    }

    [Fact]
    public void ToTsQuery_RemovesDuplicates_AndLimitsTerms()
    {
        var search = string.Join(' ', Enumerable.Range(1, 15).Select(x => $"w{x} w{x}"));

        var terms = ProductSearch.ToTsQuery(search).Split(" & ");

        Assert.Equal(ProductSearch.MaxTerms, terms.Length);
        Assert.Equal("w1:*", terms[0]);
        Assert.Equal(terms.Length, terms.Distinct().Count());
    }

    [Fact]
    public void ToTsQuery_RejectsTooLongSearch()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ProductSearch.ToTsQuery(new string('a', ProductSearch.MaxLength + 1)));

        Assert.StartsWith($"Search must not exceed {ProductSearch.MaxLength} characters.", exception.Message);
    }
}
