namespace MiniNaturalFarms.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Processing = 2,
    Dispatched = 3,
    Delivered = 4,
    Cancelled = 5
}

public enum PaymentMethod
{
    Cod = 0,
    Upi = 1,
    Card = 2,
    Online = 3
}

public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Refunded = 3
}

