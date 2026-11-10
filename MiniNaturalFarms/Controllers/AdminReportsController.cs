using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniNaturalFarms.Application.Admin;

namespace MiniNaturalFarms.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "Admin")]
public class AdminReportsController : ControllerBase
{
    private readonly IAdminReportService _reports;
    public AdminReportsController(IAdminReportService reports) => _reports = reports;

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> Dashboard(CancellationToken ct) =>
        Ok(await _reports.GetDashboardAsync(ct));

    [HttpGet("sales")]
    public async Task<ActionResult<SalesReportDto>> Sales(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? groupBy, CancellationToken ct) =>
        Ok(await _reports.GetSalesAsync(from, to, groupBy, ct));

    [HttpGet("top-products")]
    public async Task<ActionResult<List<TopProductDto>>> TopProducts(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int take = 10, CancellationToken ct = default) =>
        Ok(await _reports.GetTopProductsAsync(from, to, take, ct));

    [HttpGet("inventory")]
    public async Task<ActionResult<InventoryReportDto>> Inventory(
        [FromQuery] int lowStockThreshold = InventoryRules.DefaultLowStockThreshold, CancellationToken ct = default) =>
        Ok(await _reports.GetInventoryAsync(lowStockThreshold, ct));

    [HttpGet("customers")]
    public async Task<ActionResult<CustomerReportDto>> Customers(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int take = 10, CancellationToken ct = default) =>
        Ok(await _reports.GetCustomersAsync(from, to, take, ct));

    [HttpGet("payments")]
    public async Task<ActionResult<PaymentReportDto>> Payments(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct = default) =>
        Ok(await _reports.GetPaymentsAsync(from, to, ct));
}