using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniNaturalFarms.Application.Customer;
using MiniNaturalFarms.Helpers;

namespace MiniNaturalFarms.Controllers;

[ApiController]
[Route("api/addresses")]
[Authorize]
public class AddressesController : ControllerBase
{
    private readonly IAddressService _addresses;
    public AddressesController(IAddressService addresses) => _addresses = addresses;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AddressDto>>> GetAll(CancellationToken ct) =>
        Ok(await _addresses.GetAllAsync(User.GetUserId(), ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AddressDto>> Get(int id, CancellationToken ct) =>
        Ok(await _addresses.GetAsync(User.GetUserId(), id, ct));

    [HttpPost]
    public async Task<ActionResult<AddressDto>> Create(SaveAddressRequest request, CancellationToken ct)
    {
        var created = await _addresses.CreateAsync(User.GetUserId(), request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AddressDto>> Update(int id, SaveAddressRequest request, CancellationToken ct) =>
        Ok(await _addresses.UpdateAsync(User.GetUserId(), id, request, ct));

    [HttpPut("{id:int}/default")]
    public async Task<ActionResult<AddressDto>> SetDefault(int id, CancellationToken ct) =>
        Ok(await _addresses.SetDefaultAsync(User.GetUserId(), id, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _addresses.DeleteAsync(User.GetUserId(), id, ct);
        return NoContent();
    }
}