using System.ComponentModel.DataAnnotations;

namespace MiniNaturalFarms.Application.Customer;

public record ProfileDto(string Email, string FullName, string? PhoneNumber, DateTime CreatedAt);

public class UpdateProfileRequest
{
    [Required, MaxLength(150)] public string FullName { get; set; } = string.Empty;

    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number.")]
    public string? PhoneNumber { get; set; }
}

public class ChangePasswordRequest
{
    [Required] public string CurrentPassword { get; set; } = string.Empty;
    [Required, MinLength(8)] public string NewPassword { get; set; } = string.Empty;
}