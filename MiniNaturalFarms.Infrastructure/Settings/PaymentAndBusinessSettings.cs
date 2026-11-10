namespace MiniNaturalFarms.Infrastructure.Settings;

public class RazorpaySettings
{
    public string KeyId { get; set; } = string.Empty;
    public string KeySecret { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}

public class ShippingSettings
{
    public decimal FreeShippingThreshold { get; set; } = 500;
    public decimal FlatRate { get; set; } = 40;
}

public class BusinessSettings
{
    public string Name { get; set; } = "Mini Natural Farms";
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Gstin { get; set; } = string.Empty;
}