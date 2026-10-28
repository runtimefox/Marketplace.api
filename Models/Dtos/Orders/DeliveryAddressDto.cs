using System.ComponentModel.DataAnnotations;
using MyApi.Models.Entities;

namespace MyApi.Models.Dtos.Orders;

public record DeliveryAddressDto
{
    [Required]
    [MaxLength(DeliveryAddress.RecipientNameMaxLength)]
    public required string RecipientName { get; init; }

    [Required]
    public required string Phone { get; init; }

    [Required]
    [MaxLength(DeliveryAddress.CountryMaxLength)]
    public required string Country { get; init; }

    [Required]
    [MaxLength(DeliveryAddress.CityMaxLength)]
    public required string City { get; init; }

    [Required]
    [MaxLength(DeliveryAddress.AddressLineMaxLength)]
    public required string AddressLine { get; init; }

    [MaxLength(DeliveryAddress.ApartmentMaxLength)]
    public string? Apartment { get; init; }

    [Required]
    public required string PostalCode { get; init; }

    [MaxLength(DeliveryAddress.CommentMaxLength)]
    public string? Comment { get; init; }
}
