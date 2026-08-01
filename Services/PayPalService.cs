using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace U3_Examen_Airport.Services;

public sealed class PayPalService : IPayPalService
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _baseUrl;
    private readonly string _returnUrl;
    private readonly string _cancelUrl;

    public PayPalService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _clientId = GetRequiredConfiguration(configuration, "PayPal:ClientId");
        _clientSecret = GetRequiredConfiguration(configuration, "PayPal:ClientSecret");
        _baseUrl = GetRequiredConfiguration(configuration, "PayPal:BaseUrl").TrimEnd('/');
        _returnUrl = GetRequiredConfiguration(configuration, "PayPal:ReturnUrl");
        _cancelUrl = GetRequiredConfiguration(configuration, "PayPal:CancelUrl");
    }

    public async Task<PayPalOrderCreationResult> CreateOrderAsync(
        decimal totalAmount,
        CancellationToken cancellationToken)
    {
        if (totalAmount <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalAmount),
                "El total de la orden de PayPal debe ser mayor que cero.");
        }

        var accessToken = await GetAccessTokenAsync(cancellationToken);
        var body = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    amount = new
                    {
                        currency_code = "USD",
                        value = totalAmount.ToString("F2", CultureInfo.InvariantCulture)
                    }
                }
            },
            payment_source = new
            {
                paypal = new
                {
                    experience_context = new
                    {
                        return_url = _returnUrl,
                        cancel_url = _cancelUrl,
                        user_action = "PAY_NOW"
                    }
                }
            }
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_baseUrl}/v2/checkout/orders");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = CreateJsonContent(body);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, rawResponse, "crear la orden");

        using var document = JsonDocument.Parse(rawResponse);
        var root = document.RootElement;
        var payPalOrderId = GetRequiredString(root, "id", "PayPal no devolvió el ID de la orden.");
        var approvalUrl = FindApprovalUrl(root);

        if (string.IsNullOrWhiteSpace(approvalUrl))
        {
            throw new InvalidOperationException(
                "PayPal creó la orden, pero no devolvió una URL de aprobación.");
        }

        return new PayPalOrderCreationResult(payPalOrderId, approvalUrl);
    }

    public async Task<PayPalCaptureResult> CaptureOrderAsync(
        string payPalOrderId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payPalOrderId))
        {
            throw new ArgumentException(
                "El ID de la orden de PayPal es obligatorio.",
                nameof(payPalOrderId));
        }

        var accessToken = await GetAccessTokenAsync(cancellationToken);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_baseUrl}/v2/checkout/orders/{Uri.EscapeDataString(payPalOrderId)}/capture");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, rawResponse, "capturar la orden");

        using var document = JsonDocument.Parse(rawResponse);
        var root = document.RootElement;
        var status = GetRequiredString(root, "status", "PayPal no devolvió el estado de la captura.");

        var captureId = root
            .GetProperty("purchase_units")[0]
            .GetProperty("payments")
            .GetProperty("captures")[0]
            .GetProperty("id")
            .GetString();

        if (string.IsNullOrWhiteSpace(captureId))
        {
            throw new InvalidOperationException(
                "PayPal no devolvió el identificador de la captura.");
        }

        return new PayPalCaptureResult(status, captureId, rawResponse);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_baseUrl}/v1/oauth2/token");

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials"
            });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, rawResponse, "obtener el access token");

        using var document = JsonDocument.Parse(rawResponse);
        return GetRequiredString(
            document.RootElement,
            "access_token",
            "PayPal no devolvió un access token.");
    }

    private static StringContent CreateJsonContent<T>(T value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

    private static string GetRequiredConfiguration(
        IConfiguration configuration,
        string key)
    {
        var value = configuration[key];

        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException(
                $"Falta la configuración obligatoria '{key}'.")
            : value;
    }

    private static string GetRequiredString(
        JsonElement element,
        string propertyName,
        string errorMessage)
    {
        if (!element.TryGetProperty(propertyName, out var property)
            || string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return property.GetString()!;
    }

    private static string? FindApprovalUrl(JsonElement root)
    {
        if (!root.TryGetProperty("links", out var links)
            || links.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var link in links.EnumerateArray())
        {
            if (!link.TryGetProperty("rel", out var relProperty)
                || !link.TryGetProperty("href", out var hrefProperty))
            {
                continue;
            }

            var relation = relProperty.GetString();
            var isApprovalLink = string.Equals(
                    relation,
                    "approve",
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    relation,
                    "payer-action",
                    StringComparison.OrdinalIgnoreCase);

            if (isApprovalLink && hrefProperty.ValueKind == JsonValueKind.String)
            {
                return hrefProperty.GetString();
            }
        }

        return null;
    }

    private static void EnsureSuccess(
        HttpResponseMessage response,
        string responseBody,
        string operation)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"PayPal rechazó la operación al {operation}. " +
                $"Código HTTP {(int)response.StatusCode} ({response.StatusCode}). " +
                $"Respuesta: {responseBody}");
        }
    }
}
