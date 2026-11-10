using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniNaturalFarms.Application.Catalog;
using MiniNaturalFarms.Application.Common;
using MiniNaturalFarms.Helpers;

namespace MiniNaturalFarms.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categories;
    public CategoriesController(ICategoryService categories) => _categories = categories;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll([FromQuery] bool includeInactive, CancellationToken ct) =>
        Ok(await _categories.GetAllAsync(includeInactive && User.IsInRole("Admin"), ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDto>> Get(int id, CancellationToken ct) =>
        Ok(await _categories.GetByIdAsync(id, ct));

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryDto>> Create(SaveCategoryRequest request, CancellationToken ct)
    {
        var created = await _categories.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryDto>> Update(int id, SaveCategoryRequest request, CancellationToken ct) =>
        Ok(await _categories.UpdateAsync(id, request, ct));

    //[HttpPost("{id:int}/image"), Authorize(Roles = "Admin")]
    //[RequestSizeLimit(6 * 1024 * 1024)]
    //public async Task<ActionResult<CategoryDto>> UploadImage(int id, [FromForm] IFormFile file, CancellationToken ct)
    //{
    //    var error = UploadValidator.Validate(file);
    //    if (error != null) return BadRequest(error);

    //    await using var stream = file.OpenReadStream();
    //    return Ok(await _categories.SetImageAsync(id, new UploadFile(stream, file.FileName), ct));
    //}
    [HttpPost("{id:int}/image"), Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<CategoryDto>> UploadImage(int id, [FromForm] UploadImageRequest request, CancellationToken ct)
    {
        var error = UploadValidator.Validate(request.File);
        if (error != null) return BadRequest(error);

        await using var stream = request.File.OpenReadStream();
        return Ok(await _categories.SetImageAsync(id, new UploadFile(stream, request.File.FileName), ct));
    }
    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _categories.DeleteAsync(id, ct);
        return NoContent();
    }
}