namespace U3_Examen_Airport.Services;

public interface IPayPalService
{
    Task<PayPalOrderCreationResult> CreateOrderAsync(
        decimal totalAmount,
        CancellationToken cancellationToken);

    Task<PayPalCaptureResult> CaptureOrderAsync(
        string payPalOrderId,
        CancellationToken cancellationToken);
}

public sealed record PayPalOrderCreationResult(
    string PayPalOrderId,
    string ApprovalUrl);

public sealed record PayPalCaptureResult(
    string Status,
    string CaptureId,
    string RawResponse);
