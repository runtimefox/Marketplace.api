using MyApi.Models.Entities;

namespace MyApi.Tests.Unit;

public class DeliveryAddressEntityTests
{
    [Fact]
    public void Constructor_NormalizesValues()
    {
        var address = new DeliveryAddress(
            "  John Smith ",
            "+44 (20) 7946-0958",
            " United Kingdom ",
            " London ",
            "10 Downing Street",
            "   ",
            " sw1a 2aa ",
            "");

        Assert.Equal("John Smith", address.RecipientName);
        Assert.Equal("+442079460958", address.Phone);
        Assert.Equal("United Kingdom", address.Country);
        Assert.Equal("London", address.City);
        Assert.Equal("SW1A 2AA", address.PostalCode);
        Assert.Null(address.Apartment);
        Assert.Null(address.Comment);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("+1 415 abc 0142")]
    [InlineData("+1234567890123456")]
    public void Constructor_WithInvalidPhone_Throws(string phone)
    {
        Assert.Throws<ArgumentException>(() => NewAddress(phone: phone));
    }

    [Theory]
    [InlineData("12")]
    [InlineData("12345678901")]
    [InlineData("!@#$")]
    public void Constructor_WithInvalidPostalCode_Throws(string postalCode)
    {
        Assert.Throws<ArgumentException>(() => NewAddress(postalCode: postalCode));
    }

    [Theory]
    [InlineData(" ", "San Francisco")]
    [InlineData("United States", " ")]
    public void Constructor_WithEmptyCountryOrCity_Throws(string country, string city)
    {
        Assert.Throws<ArgumentException>(() => NewAddress(country: country, city: city));
    }

    private static DeliveryAddress NewAddress(
        string phone = "+14155550142",
        string country = "United States",
        string city = "San Francisco",
        string postalCode = "94105") =>
        new("Test Buyer", phone, country, city, "500 Market Street", null, postalCode, null);
}
