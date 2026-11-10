using Microsoft.AspNetCore.Identity;
using MiniNaturalFarms.Application.Common;
using MiniNaturalFarms.Application.Customer;
using MiniNaturalFarms.Infrastructure.Identity;

namespace MiniNaturalFarms.Infrastructure.Customer;

public class ProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _users;

    public ProfileService(UserManager<ApplicationUser> users) => _users = users;

    public async Task<ProfileDto> GetAsync(string userId, CancellationToken ct = default) =>
        ToDto(await FindAsync(userId));

    public async Task<ProfileDto> UpdateAsync(string userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await FindAsync(userId);
        user.FullName = request.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

        var result = await _users.UpdateAsync(user);
        EnsureSucceeded(result);
        return ToDto(user);
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await FindAsync(userId);
        var result = await _users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        EnsureSucceeded(result);
    }

    private async Task<ApplicationUser> FindAsync(string userId) =>
        await _users.FindByIdAsync(userId) ?? throw new NotFoundException("User not found.");

    private static ProfileDto ToDto(ApplicationUser u) =>
        new(u.Email ?? string.Empty, u.FullName, u.PhoneNumber, u.CreatedAt);

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));
    }
}