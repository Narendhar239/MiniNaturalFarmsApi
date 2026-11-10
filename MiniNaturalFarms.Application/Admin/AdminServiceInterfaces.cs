using MiniNaturalFarms.Application.Common;

namespace MiniNaturalFarms.Application.Admin;

public interface IAdminOrderService
{
    Task<PagedResult<AdminOrderSummaryDto>> GetPagedAsync(AdminOrderQuery query, CancellationToken ct = default);
    Task<AdminOrderDto> GetAsync(int id, CancellationToken ct = default);
    Task<AdminOrderDto> UpdateStatusAsync(int id, UpdateOrderStatusRequest request, CancellationToken ct = default);
    Task<AdminOrderDto> RefundAsync(int id, CancellationToken ct = default);
}

public interface IAdminInventoryService
{
    Task<PagedResult<InventoryItemDto>> GetAsync(InventoryQuery query, CancellationToken ct = default);
    Task<InventoryItemDto> SetStockAsync(int productId, int quantity, CancellationToken ct = default);
}

public interface IAdminCustomerService
{
    Task<PagedResult<CustomerSummaryDto>> GetPagedAsync(CustomerQuery query, CancellationToken ct = default);
    Task<CustomerDetailDto> GetAsync(string id, CancellationToken ct = default);
}

public interface IAdminReportService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default);
    Task<SalesReportDto> GetSalesAsync(DateOnly? from, DateOnly? to, string? groupBy, CancellationToken ct = default);
    Task<List<TopProductDto>> GetTopProductsAsync(DateOnly? from, DateOnly? to, int take, CancellationToken ct = default);
    Task<InventoryReportDto> GetInventoryAsync(int lowStockThreshold, CancellationToken ct = default);
    Task<CustomerReportDto> GetCustomersAsync(DateOnly? from, DateOnly? to, int take, CancellationToken ct = default);
    Task<PaymentReportDto> GetPaymentsAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default);
}