using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniNaturalFarms.Application.Catalog;
using MiniNaturalFarms.Application.Common;
using MiniNaturalFarms.Helpers;

namespace MiniNaturalFarms.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _products;
    public ProductsController(IProductService products) => _products = products;

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetPaged([FromQuery] ProductQuery query, CancellationToken ct)
    {
        query.IncludeInactive = query.IncludeInactive && User.IsInRole("Admin");
        return Ok(await _products.GetPagedAsync(query, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> Get(int id, CancellationToken ct) =>
        Ok(await _products.GetByIdAsync(id, User.IsInRole("Admin"), ct));

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> Create(SaveProductRequest request, CancellationToken ct)
    {
        var created = await _products.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> Update(int id, SaveProductRequest request, CancellationToken ct) =>
        Ok(await _products.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await _products.DeactivateAsync(id, ct);
        return NoContent();
    }

    //[HttpPost("{id:int}/images"), Authorize(Roles = "Admin")]
    //[RequestSizeLimit(42 * 1024 * 1024)]
    //public async Task<ActionResult<ProductDto>> AddImages(int id, [FromForm] List<IFormFile> files, CancellationToken ct)
    //{
    //    if (files.Count == 0) return BadRequest("Attach at least one image in the 'files' field.");

    //    foreach (var f in files)
    //    {
    //        var error = UploadValidator.Validate(f);
    //        if (error != null) return BadRequest(error);
    //    }

    //    var streams = new List<Stream>();
    //    try
    //    {
    //        var uploads = files.Select(f =>
    //        {
    //            var s = f.OpenReadStream();
    //            streams.Add(s);
    //            return new UploadFile(s, f.FileName);
    //        }).ToList();

    //        return Ok(await _products.AddImagesAsync(id, uploads, ct));
    //    }
    //    finally
    //    {
    //        foreach (var s in streams) await s.DisposeAsync();
    //    }
    //}
    [HttpPost("{id:int}/images"), Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(42 * 1024 * 1024)]
    public async Task<ActionResult<ProductDto>> AddImages(int id, [FromForm] UploadImagesRequest request, CancellationToken ct)
    {
        var files = request.Files;
        if (files.Count == 0) return BadRequest("Attach at least one image in the 'Files' field.");

        foreach (var f in files)
        {
            var error = UploadValidator.Validate(f);
            if (error != null) return BadRequest(error);
        }

        var streams = new List<Stream>();
        try
        {
            var uploads = files.Select(f =>
            {
                var s = f.OpenReadStream();
                streams.Add(s);
                return new UploadFile(s, f.FileName);
            }).ToList();

            return Ok(await _products.AddImagesAsync(id, uploads, ct));
        }
        finally
        {
            foreach (var s in streams) await s.DisposeAsync();
        }
    }
    [HttpDelete("{id:int}/images/{imageId:int}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> DeleteImage(int id, int imageId, CancellationToken ct) =>
        Ok(await _products.DeleteImageAsync(id, imageId, ct));

    [HttpPut("{id:int}/images/{imageId:int}/primary"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> SetPrimary(int id, int imageId, CancellationToken ct) =>
        Ok(await _products.SetPrimaryImageAsync(id, imageId, ct));
}