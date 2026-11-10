using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniNaturalFarms.Application.Customer;
using MiniNaturalFarms.Helpers;

namespace MiniNaturalFarms.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profile;
    public ProfileController(IProfileService profile) => _profile = profile;

    [HttpGet]
    public async Task<ActionResult<ProfileDto>> Get(CancellationToken ct) =>
        Ok(await _profile.GetAsync(User.GetUserId(), ct));

    [HttpPut]
    public async Task<ActionResult<ProfileDto>> Update(UpdateProfileRequest request, CancellationToken ct) =>
        Ok(await _profile.UpdateAsync(User.GetUserId(), request, ct));

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await _profile.ChangePasswordAsync(User.GetUserId(), request, ct);
        return NoContent();
    }
}