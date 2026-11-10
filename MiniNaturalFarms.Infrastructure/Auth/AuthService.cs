using Microsoft.AspNetCore.Identity;
using MiniNaturalFarms.Application.Auth;
using MiniNaturalFarms.Domain.Constants;
using MiniNaturalFarms.Infrastructure.Identity;

namespace MiniNaturalFarms.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly JwtTokenService _jwt;

    public AuthService(UserManager<ApplicationUser> users, JwtTokenService jwt)
    {
        _users = users;
        _jwt = jwt;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName
        };

        var result = await _users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return AuthResult.Fail(result.Errors.Select(e => e.Description));

        await _users.AddToRoleAsync(user, AppRoles.Customer);
        return AuthResult.Ok(await BuildResponseAsync(user));
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user = await _users.FindByEmailAsync(request.Email);
        if (user is null || !await _users.CheckPasswordAsync(user, request.Password))
            return AuthResult.Fail(new[] { "Invalid email or password" });

        return AuthResult.Ok(await BuildResponseAsync(user));
    }

    private async Task<AuthResponse> BuildResponseAsync(ApplicationUser user)
    {
        var roles = (await _users.GetRolesAsync(user)).ToList();
        var (token, expiresAt) = _jwt.CreateToken(user, roles);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            Roles = roles
        };
    }
}
