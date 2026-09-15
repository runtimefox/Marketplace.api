using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApi.Models.Entities;

[Table("UserAccounts")]

public class UserAccount
{
    public const int UsernameMaxLength = 100;
    public const int EmailMaxLength = 100;
    public const int PasswordMaxLength = 255;
    public const int PasswordMinLength = 8;
    public const int RawPasswordMaxLength = 128;

    [Key]
    [Column("Id")]
    public Guid Id { get; private set; }

    [Required]
    [MaxLength(UsernameMaxLength)]
    [Column("Username")]
    public string Username { get; private set; } = null!;

    [Required]
    [MaxLength(EmailMaxLength)]
    [Column("Email")]
    public string Email { get; private set; } = null!;

    [Required]
    [MaxLength(PasswordMaxLength)]
    [Column("Password")]
    public string PasswordHash { get; private set; } = null!;

    [Column("Role")]
    public UserRole Role { get; private set; }

    [MaxLength(ImageStorageKey.MaxLength)]
    [Column("AvatarKey")]
    public string? AvatarKey { get; private set; }

    private UserAccount()
    {
    }

    public UserAccount(string username, string email, string passwordHash)
    {
        Id = Guid.CreateVersion7();
        ChangeUsername(username);
        ChangeEmail(email);
        SetPasswordHash(passwordHash);
        Role = UserRole.Customer;
    }

    public void ChangeUsername(string username)
    {
        Username = Require(username, UsernameMaxLength, nameof(username));
    }

    public void ChangeEmail(string email)
    {
        email = Require(email, EmailMaxLength, nameof(email));

        if (!new EmailAddressAttribute().IsValid(email))
        {
            throw new ArgumentException("Invalid email.", nameof(email));
        }

        Email = email;
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = Require(passwordHash, PasswordMaxLength, nameof(passwordHash));
    }

    public void ChangeAvatar(string storageKey)
    {
        AvatarKey = ImageStorageKey.Validate(storageKey, nameof(storageKey));
    }

    public void RemoveAvatar()
    {
        AvatarKey = null;
    }

    public void ChangeRole(UserRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role), "Unknown user role.");
        }

        Role = role;
    }

    private static string Require(string value, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", paramName);
        }

        value = value.Trim();

        if (value.Length > maxLength)
        {
            throw new ArgumentException($"Length must not exceed {maxLength} characters.", paramName);
        }

        return value;
    }
}
