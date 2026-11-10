namespace MiniNaturalFarms.Application.Admin;

public record SalesPointDto(string Period, int Orders, int Units, decimal Revenue, decimal AverageOrderValue);

public record SalesReportDto(
    DateOnly From, DateOnly To, string GroupBy,
    int TotalOrders, int TotalUnits, decimal TotalRevenue, List<SalesPointDto> Rows);

public record TopProductDto(int ProductId, string Name, int UnitsSold, decimal Revenue);

public record DashboardDto(
    int TodayOrders, decimal TodayRevenue, int MonthOrders, decimal MonthRevenue,
    Dictionary<string, int> OrdersByStatus,
    int TotalCustomers, int NewCustomersThisMonth,
    int LowStockCount, int OutOfStockCount,
    List<SalesPointDto> Last30Days, List<TopProductDto> TopProducts, List<AdminOrderSummaryDto> RecentOrders);

public record InventoryReportDto(
    int TotalProducts, int ActiveProducts, int OutOfStock, int LowStock, int LowStockThreshold,
    decimal StockValue, List<InventoryItemDto> LowStockItems);

public record TopCustomerDto(string UserId, string FullName, string Email, int Orders, decimal TotalSpent);

public record CustomerReportDto(
    DateOnly From, DateOnly To, int TotalCustomers, int NewCustomers, List<TopCustomerDto> TopCustomers);

public record PaymentBreakdownDto(string Method, string Status, int Orders, decimal Amount);

public record PaymentReportDto(
    DateOnly From, DateOnly To,
    decimal Collected, decimal Refunded, decimal AwaitingRefund, decimal ToCollect,
    List<PaymentBreakdownDto> Breakdown);