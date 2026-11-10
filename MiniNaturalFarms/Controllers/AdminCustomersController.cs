using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniNaturalFarms.Application.Admin;
using MiniNaturalFarms.Application.Common;

namespace MiniNaturalFarms.Controllers;

[ApiController]
[Route("api/admin/customers")]
[Authorize(Roles = "Admin")]
public class AdminCustomersController : ControllerBase
{
    private readonly IAdminCustomerService _customers;
    public AdminCustomersController(IAdminCustomerService customers) => _customers = customers;

    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerSummaryDto>>> GetPaged([FromQuery] CustomerQuery query, CancellationToken ct) =>
        Ok(await _customers.GetPagedAsync(query, ct));

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDetailDto>> Get(string id, CancellationToken ct) =>
        Ok(await _customers.GetAsync(id, ct));
}