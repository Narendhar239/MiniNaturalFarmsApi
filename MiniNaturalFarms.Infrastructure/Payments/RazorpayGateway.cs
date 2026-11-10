using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniNaturalFarms.Application.Common;
using MiniNaturalFarms.Application.Orders;
using MiniNaturalFarms.Infrastructure.Settings;

namespace MiniNaturalFarms.Infrastructure.Payments;

public class RazorpayGateway : IPaymentGateway
{
    private readonly HttpClient _http;
    private readonly RazorpaySettings _settings;
    private readonly ILogger<RazorpayGateway> _logger;

    public RazorpayGateway(HttpClient http, IOptions<RazorpaySettings> options, ILogger<RazorpayGateway> logger)
    {
        _http = http;
        _settings = options.Value;
        _logger = logger;

        _http.BaseAddress = new Uri("https://api.razorpay.com/v1/");
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.KeyId}:{_settings.KeySecret}"));
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
    }

    public string KeyId => _settings.KeyId;

    public async Task<string> CreateOrderAsync(long amountPaise, string receipt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.KeyId) || string.IsNullOrWhiteSpace(_settings.KeySecret))
            throw new ExternalServiceException("Online payments are not configured yet. Please choose Cash on Delivery.");

        var payload = new { amount = amountPaise, currency = "INR", receipt };

        using var response = await _http.PostAsJsonAsync("orders", payload, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Razorpay order creation failed ({Status}): {Body}", (int)response.StatusCode, body);
            throw new ExternalServiceException("Could not start the online payment. Please try again or choose Cash on Delivery.");
        }

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("id").GetString()
               ?? throw new ExternalServiceException("Payment gateway returned an invalid response.");
    }

    public async Task<string> RefundAsync(string razorpayPaymentId, long amountPaise, string receipt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.KeyId) || string.IsNullOrWhiteSpace(_settings.KeySecret))
            throw new ExternalServiceException("Online payments are not configured, so a refund can't be issued.");

        var payload = new { amount = amountPaise, speed = "normal", receipt };

        using var response = await _http.PostAsJsonAsync($"payments/{razorpayPaymentId}/refund", payload, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Razorpay refund failed ({Status}): {Body}", (int)response.StatusCode, body);
            throw new ExternalServiceException("The refund could not be processed. Please try again, or refund it from the Razorpay dashboard.");
        }

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("id").GetString()
               ?? throw new ExternalServiceException("Payment gateway returned an invalid response.");
    }

    public bool VerifyPaymentSignature(string razorpayOrderId, string razorpayPaymentId, string signature) =>
        IsValidHmac($"{razorpayOrderId}|{razorpayPaymentId}", signature, _settings.KeySecret);

    public bool VerifyWebhookSignature(string rawBody, string signature) =>
        IsValidHmac(rawBody, signature, _settings.WebhookSecret);

    private static bool IsValidHmac(string payload, string signature, string secret)
    {
        if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(secret)) return false;

        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload));
        var expected = Convert.ToHexStringLower(hash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature.Trim().ToLowerInvariant()));
    }
}