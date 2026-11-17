using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MiniNaturalFarms.Application.Auth;
using MiniNaturalFarms.Application.Common;
using MiniNaturalFarms.Domain.Constants;
using MiniNaturalFarms.Infrastructure.Identity;

namespace MiniNaturalFarms.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly JwtTokenService _jwt;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(UserManager<ApplicationUser> users, JwtTokenService jwt, IEmailService email,
        IConfiguration config, ILogger<AuthService> logger)
    {
        _users = users;
        _jwt = jwt;
        _email = email;
        _config = config;
        _logger = logger;
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

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _users.FindByEmailAsync(request.Email.Trim());
        if (user is null || string.IsNullOrEmpty(user.Email))
            return;

        try
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var baseUrl = (_config["Frontend:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
            var link = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}";
            var shop = _config["Business:Name"] ?? "Mini Natural Farms";
            var safeLink = WebUtility.HtmlEncode(link);

            var body = $"""
            <p>Hello {WebUtility.HtmlEncode(user.FullName)},</p>
            <p>We received a request to reset your {WebUtility.HtmlEncode(shop)} password. Click the button below to choose a new one. This link works for 1 hour.</p>
            <p><a href="{safeLink}" style="display:inline-block;padding:10px 20px;background:#2e7d32;color:#ffffff;text-decoration:none;border-radius:6px;">Reset password</a></p>
            <p>If the button does not work, copy this link into your browser:<br>{safeLink}</p>
            <p>If you did not ask for this, you can ignore this email. Your password will not change.</p>
            """;

            await _email.SendAsync(user.Email, $"Reset your {shop} password", body, ct);
        }
        catch (Exception ex)
        {
            // Same response either way, so the page can't be used to discover which emails exist
            _logger.LogError(ex, "Could not send a password reset email.");
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        const string invalid = "This reset link is invalid or has expired. Please request a new one.";

        var user = await _users.FindByEmailAsync(request.Email.Trim())
            ?? throw new BadRequestException(invalid);

        var result = await _users.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (result.Succeeded)
            return;

        if (result.Errors.Any(e => e.Code == "InvalidToken"))
            throw new BadRequestException(invalid);

        throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));
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
