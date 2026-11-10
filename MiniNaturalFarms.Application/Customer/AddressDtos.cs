using System.ComponentModel.DataAnnotations;

namespace MiniNaturalFarms.Application.Customer;

public record AddressDto(
    int Id, string FullName, string Phone, string Line1, string? Line2,
    string City, string State, string Pincode, bool IsDefault);

public class SaveAddressRequest
{
    [Required, MaxLength(150)] public string FullName { get; set; } = string.Empty;

    [Required, RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number.")]
    public string Phone { get; set; } = string.Empty;

    [Required, MaxLength(250)] public string Line1 { get; set; } = string.Empty;
    [MaxLength(250)] public string? Line2 { get; set; }
    [Required, MaxLength(100)] public string City { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string State { get; set; } = string.Empty;

    [Required, RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode must be 6 digits.")]
    public string Pincode { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}