using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace MyApi.Models.Entities;

public sealed partial class DeliveryAddress
{
    public const int RecipientNameMaxLength = 100;
    public const int PhoneMaxLength = 16;
    public const int CountryMaxLength = 60;
    public const int CityMaxLength = 100;
    public const int AddressLineMaxLength = 200;
    public const int ApartmentMaxLength = 20;
    public const int PostalCodeMaxLength = 10;
    public const int CommentMaxLength = 500;

    [MaxLength(RecipientNameMaxLength)]
    [Column("DeliveryRecipientName")]
    public string RecipientName { get; private set; } = null!;

    [MaxLength(PhoneMaxLength)]
    [Column("DeliveryPhone")]
    public string Phone { get; private set; } = null!;

    [MaxLength(CountryMaxLength)]
    [Column("DeliveryCountry")]
    public string Country { get; private set; } = null!;

    [MaxLength(CityMaxLength)]
    [Column("DeliveryCity")]
    public string City { get; private set; } = null!;

    [MaxLength(AddressLineMaxLength)]
    [Column("DeliveryAddressLine")]
    public string AddressLine { get; private set; } = null!;

    [MaxLength(ApartmentMaxLength)]
    [Column("DeliveryApartment")]
    public string? Apartment { get; private set; }

    [MaxLength(PostalCodeMaxLength)]
    [Column("DeliveryPostalCode")]
    public string PostalCode { get; private set; } = null!;

    [MaxLength(CommentMaxLength)]
    [Column("DeliveryComment")]
    public string? Comment { get; private set; }

    private DeliveryAddress()
    {
    }

    public DeliveryAddress(
        string recipientName,
        string phone,
        string country,
        string city,
        string addressLine,
        string? apartment,
        string postalCode,
        string? comment)
    {
        RecipientName = Required(recipientName, RecipientNameMaxLength, nameof(recipientName));
        Phone = NormalizePhone(phone);
        Country = Required(country, CountryMaxLength, nameof(country));
        City = Required(city, CityMaxLength, nameof(city));
        AddressLine = Required(addressLine, AddressLineMaxLength, nameof(addressLine));
        Apartment = Optional(apartment, ApartmentMaxLength, nameof(apartment));
        PostalCode = NormalizePostalCode(postalCode);
        Comment = Optional(comment, CommentMaxLength, nameof(comment));
    }

    private static string NormalizePhone(string phone)
    {
        var normalized = PhoneSeparators().Replace(phone ?? string.Empty, string.Empty);

        if (!PhoneFormat().IsMatch(normalized))
        {
            throw new ArgumentException("Phone must contain 10 to 15 digits and may start with +.", nameof(phone));
        }

        return normalized;
    }

    private static string NormalizePostalCode(string postalCode)
    {
        var normalized = (postalCode ?? string.Empty).Trim().ToUpperInvariant();

        if (!PostalCodeFormat().IsMatch(normalized))
        {
            throw new ArgumentException(
                "Postal code must be 3 to 10 letters, digits, spaces or hyphens.", nameof(postalCode));
        }

        return normalized;
    }

    private static string Required(string value, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", paramName);
        }

        return Optional(value, maxLength, paramName)!;
    }

    private static string? Optional(string? value, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();

        if (value.Length > maxLength)
        {
            throw new ArgumentException($"Length must not exceed {maxLength} characters.", paramName);
        }

        return value;
    }

    [GeneratedRegex(@"[\s\-()]")]
    private static partial Regex PhoneSeparators();

    [GeneratedRegex(@"^\+?[0-9]{10,15}$")]
    private static partial Regex PhoneFormat();

    [GeneratedRegex(@"^[A-Z0-9][A-Z0-9 \-]{1,8}[A-Z0-9]$")]
    private static partial Regex PostalCodeFormat();
}
