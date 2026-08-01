using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using U3_Examen_Airport.Data;
using U3_Examen_Airport.Models.Application;
using Microsoft.AspNetCore.Authorization;
using U3_Examen_Airport.Services;

namespace U3_Examen_Airport.Controllers
{
    [Authorize]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AirportContext _airportContext;
        private readonly IPayPalService _payPalService;

        public PaymentsController(
            ApplicationDbContext context,
            AirportContext airportContext,
            IPayPalService payPalService)
        {
            _context = context;
            _airportContext = airportContext;
            _payPalService = payPalService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> SelectGateway(
            int orderId,
            CancellationToken cancellationToken)
        {
            if (orderId <= 0)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.FlightChangeRequest)
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(
                    o => o.OrderId == orderId,
                    cancellationToken);

            if (order is null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var canAccess = !string.IsNullOrWhiteSpace(userId)
                && (order.UserId == userId || User.IsInRole("Administrador"));

            if (!canAccess)
            {
                return Forbid();
            }

            if (!string.Equals(
                    order.Status,
                    "Pendiente",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "Solo las órdenes pendientes pueden iniciar un pago.";

                return RedirectToAction(
                    "Details",
                    "Orders",
                    new { id = order.OrderId });
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [NonAction]
        public async Task<IActionResult> StartPaymentLegacy(
            int orderId,
            string gateway,
            CancellationToken cancellationToken)
        {
            var selectedGateway = gateway?.Trim();

            if (!string.Equals(
                    selectedGateway,
                    "PayPal",
                    StringComparison.OrdinalIgnoreCase)
                && !string.Equals(
                    selectedGateway,
                    "PayPhone",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("La pasarela seleccionada no es válida.");
            }

            selectedGateway = string.Equals(
                selectedGateway,
                "PayPal",
                StringComparison.OrdinalIgnoreCase)
                ? "PayPal"
                : "PayPhone";

            var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    o => o.OrderId == orderId,
                    cancellationToken);

            if (order is null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var canAccess = !string.IsNullOrWhiteSpace(userId)
                && (order.UserId == userId || User.IsInRole("Administrador"));

            if (!canAccess)
            {
                return Forbid();
            }

            if (!string.Equals(
                    order.Status,
                    "Pendiente",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "La orden ya no se encuentra pendiente.";

                return RedirectToAction(
                    "Details",
                    "Orders",
                    new { id = order.OrderId });
            }

            TempData["OrderId"] = order.OrderId;
            TempData["Gateway"] = selectedGateway;

            return View("PaymentPending");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> StartPayment(
            int orderId,
            string gateway,
            CancellationToken cancellationToken)
        {
            if (!string.Equals(
                    gateway?.Trim(),
                    "PayPal",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Por el momento solo PayPal está disponible.");
            }

            var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    o => o.OrderId == orderId,
                    cancellationToken);

            if (order is null)
            {
                return NotFound();
            }

            if (!CanAccess(order.UserId))
            {
                return Forbid();
            }

            if (!string.Equals(
                    order.Status,
                    "Pendiente",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "La orden ya no se encuentra pendiente.";
                return RedirectToAction("Details", "Orders", new { id = order.OrderId });
            }

            if (order.TotalAmount <= 0m
                || !string.Equals(order.Currency, "USD", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("La orden debe tener un total positivo expresado en USD.");
            }

            PayPalOrderCreationResult payPalOrder;

            try
            {
                payPalOrder = await _payPalService.CreateOrderAsync(
                    order.TotalAmount,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                var failedPayment = await RegisterFailedPaymentAttemptAsync(
                    order,
                    exception.Message,
                    cancellationToken);

                ViewData["ErrorMessage"] =
                    "No fue posible iniciar el pago en PayPal. La orden sigue pendiente y puede intentarlo nuevamente.";

                return View("PayPalError", failedPayment);
            }

            var duplicateExists = await _context.Payments
                .AsNoTracking()
                .AnyAsync(
                    p => p.ExternalTransactionId == payPalOrder.PayPalOrderId,
                    cancellationToken);

            if (duplicateExists)
            {
                throw new InvalidOperationException(
                    "La orden de PayPal ya se encuentra registrada.");
            }

            var payment = new Payment
            {
                OrderId = order.OrderId,
                UserId = order.UserId,
                Gateway = "PayPal",
                ExternalTransactionId = payPalOrder.PayPalOrderId,
                Amount = order.TotalAmount,
                Currency = order.Currency,
                Status = "Pendiente",
                CreationDate = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync(cancellationToken);

            return Redirect(payPalOrder.ApprovalUrl);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> PayPalSuccess(
            string token,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest("PayPal no devolvió el identificador de la orden.");
            }

            var payment = await _context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o!.FlightChangeRequest)
                .FirstOrDefaultAsync(
                    p => p.ExternalTransactionId == token,
                    cancellationToken);

            if (payment?.Order?.FlightChangeRequest is null)
            {
                return NotFound();
            }

            if (!CanAccess(payment.UserId))
            {
                return Forbid();
            }

            if (string.Equals(payment.Status, "Aprobado", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Receipt), new { paymentId = payment.PaymentId });
            }

            if (!string.Equals(payment.Status, "Pendiente", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("El pago ya no se encuentra pendiente.");
            }

            PayPalCaptureResult capture;

            try
            {
                capture = await _payPalService.CaptureOrderAsync(token, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                await SetTerminalPaymentStatusAsync(
                    payment,
                    "Fallido",
                    $"FAIL-{token}",
                    exception.Message,
                    "Ocurrió un error técnico al capturar el pago en PayPal.",
                    cancellationToken);

                ViewData["ErrorMessage"] =
                    "PayPal no pudo confirmar el pago por un error técnico. No se modificó la reserva.";

                return View("PayPalError", payment);
            }

            if (!string.Equals(capture.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
            {
                await SetTerminalPaymentStatusAsync(
                    payment,
                    "Rechazado",
                    $"REJECT-{token}",
                    capture.RawResponse,
                    $"PayPal devolvió el estado {capture.Status}.",
                    cancellationToken);

                ViewData["ErrorMessage"] =
                    $"PayPal no aprobó el pago. Estado recibido: {capture.Status}. No se modificó la reserva.";

                return View("PayPalError", payment);
            }

            var transactionExists = await _context.TransactionHistories
                .AsNoTracking()
                .AnyAsync(
                    t => t.ExternalTransactionId == capture.CaptureId,
                    cancellationToken);

            if (transactionExists)
            {
                return Conflict("La captura de PayPal ya fue registrada.");
            }

            var order = payment.Order;
            var changeRequest = order.FlightChangeRequest;
            var previousStatus = changeRequest.Status;
            var confirmationDate = DateTime.UtcNow;

            await using (var transaction = await _context.Database
                .BeginTransactionAsync(cancellationToken))
            {
                try
                {
                    payment.Status = "Aprobado";
                    payment.ConfirmationDate = confirmationDate;
                    payment.ResponseMessage = LimitLength(capture.RawResponse, 1000);
                    order.Status = "Aprobado";
                    changeRequest.Status = "Aprobado";

                    _context.TransactionHistories.Add(new TransactionHistory
                    {
                        PaymentId = payment.PaymentId,
                        ExternalTransactionId = capture.CaptureId,
                        TransactionDate = confirmationDate,
                        Status = "Aprobado",
                        Amount = payment.Amount,
                        Gateway = "PayPal",
                        ResponseData = capture.RawResponse
                    });

                    _context.FlightChangeHistories.Add(new FlightChangeHistory
                    {
                        FlightChangeRequestId = changeRequest.FlightChangeRequestId,
                        PreviousStatus = previousStatus,
                        NewStatus = "Aprobado",
                        ChangeDate = confirmationDate,
                        ChangedBy = payment.UserId,
                        Observation = $"Pago PayPal aprobado. Captura: {capture.CaptureId}"
                    });

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    throw;
                }
            }

            try
            {
                var booking = await _airportContext.Bookings
                    .FirstOrDefaultAsync(
                        b => b.BookingId == changeRequest.BookingId,
                        cancellationToken);

                if (booking is null)
                {
                    TempData["WarningMessage"] =
                        "El pago fue aprobado, pero no se encontró la reserva para actualizar el vuelo.";
                }
                else
                {
                    booking.FlightId = changeRequest.NewFlightId;
                    await _airportContext.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception)
            {
                TempData["WarningMessage"] =
                    "El pago fue aprobado, pero no fue posible actualizar la reserva. Requiere revisión administrativa.";
            }

            return RedirectToAction(nameof(Receipt), new { paymentId = payment.PaymentId });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> PayPalCancel(
            string token,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest("PayPal no devolvió el identificador de la orden.");
            }

            var payment = await _context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o!.FlightChangeRequest)
                .FirstOrDefaultAsync(
                    p => p.ExternalTransactionId == token,
                    cancellationToken);

            if (payment?.Order?.FlightChangeRequest is null)
            {
                return NotFound();
            }

            if (!CanAccess(payment.UserId))
            {
                return Forbid();
            }

            if (string.Equals(payment.Status, "Pendiente", StringComparison.OrdinalIgnoreCase))
            {
                var cancellationId = $"CANCEL-{token}";
                var cancellationExists = await _context.TransactionHistories
                    .AsNoTracking()
                    .AnyAsync(
                        t => t.ExternalTransactionId == cancellationId,
                        cancellationToken);

                var cancellationDate = DateTime.UtcNow;
                payment.Status = "Cancelado";
                payment.ConfirmationDate = cancellationDate;
                payment.ResponseMessage = "El usuario canceló el proceso en PayPal.";
                payment.Order.Status = "Cancelado";
                var previousStatus = payment.Order.FlightChangeRequest.Status;
                payment.Order.FlightChangeRequest.Status = "Cancelado";

                if (!cancellationExists)
                {
                    _context.TransactionHistories.Add(new TransactionHistory
                    {
                        PaymentId = payment.PaymentId,
                        ExternalTransactionId = cancellationId,
                        TransactionDate = cancellationDate,
                        Status = "Cancelado",
                        Amount = payment.Amount,
                        Gateway = "PayPal",
                        ResponseData = "El usuario regresó mediante la URL de cancelación de PayPal."
                    });
                }

                _context.FlightChangeHistories.Add(new FlightChangeHistory
                {
                    FlightChangeRequestId = payment.Order.FlightChangeRequestId,
                    PreviousStatus = previousStatus,
                    NewStatus = "Cancelado",
                    ChangeDate = cancellationDate,
                    ChangedBy = payment.UserId,
                    Observation = "El usuario canceló el proceso de pago en PayPal."
                });

                await _context.SaveChangesAsync(cancellationToken);
            }

            return View(payment);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Receipt(
            int paymentId,
            CancellationToken cancellationToken)
        {
            var payment = await _context.Payments
                .AsNoTracking()
                .Include(p => p.Order)
                .ThenInclude(o => o!.FlightChangeRequest)
                .FirstOrDefaultAsync(
                    p => p.PaymentId == paymentId,
                    cancellationToken);

            if (payment?.Order?.FlightChangeRequest is null)
            {
                return NotFound();
            }

            if (!CanAccess(payment.UserId))
            {
                return Forbid();
            }

            return View(payment);
        }

        // GET: Payments
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Payments.Include(p => p.Order);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Payments/Details/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(m => m.PaymentId == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // GET: Payments/Create
        [Authorize(Roles = "Administrador")]
        public IActionResult Create()
        {
            ViewData["OrderId"] = new SelectList(_context.Orders, "OrderId", "Currency");
            return View();
        }

        // POST: Payments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create([Bind("PaymentId,OrderId,UserId,Gateway,ExternalTransactionId,Amount,Currency,Status,CreationDate,ConfirmationDate,ResponseMessage")] Payment payment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(payment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["OrderId"] = new SelectList(_context.Orders, "OrderId", "Currency", payment.OrderId);
            return View(payment);
        }

        // GET: Payments/Edit/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }
            ViewData["OrderId"] = new SelectList(_context.Orders, "OrderId", "Currency", payment.OrderId);
            return View(payment);
        }

        // POST: Payments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id, [Bind("PaymentId,OrderId,UserId,Gateway,ExternalTransactionId,Amount,Currency,Status,CreationDate,ConfirmationDate,ResponseMessage")] Payment payment)
        {
            if (id != payment.PaymentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(payment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaymentExists(payment.PaymentId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["OrderId"] = new SelectList(_context.Orders, "OrderId", "Currency", payment.OrderId);
            return View(payment);
        }

        // GET: Payments/Delete/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(m => m.PaymentId == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // POST: Payments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.PaymentId == id);
        }

        private bool CanAccess(string ownerUserId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return !string.IsNullOrWhiteSpace(userId)
                && (ownerUserId == userId || User.IsInRole("Administrador"));
        }

        private async Task<Payment> RegisterFailedPaymentAttemptAsync(
            Order order,
            string responseMessage,
            CancellationToken cancellationToken)
        {
            var failureId = $"FAIL-{Guid.NewGuid():N}";
            var failureDate = DateTime.UtcNow;
            var payment = new Payment
            {
                OrderId = order.OrderId,
                UserId = order.UserId,
                Gateway = "PayPal",
                ExternalTransactionId = failureId,
                Amount = order.TotalAmount,
                Currency = order.Currency,
                Status = "Fallido",
                CreationDate = failureDate,
                ConfirmationDate = failureDate,
                ResponseMessage = LimitLength(responseMessage, 1000)
            };

            _context.Payments.Add(payment);
            _context.TransactionHistories.Add(new TransactionHistory
            {
                Payment = payment,
                ExternalTransactionId = failureId,
                TransactionDate = failureDate,
                Status = "Fallido",
                Amount = payment.Amount,
                Gateway = "PayPal",
                ResponseData = responseMessage
            });

            await _context.SaveChangesAsync(cancellationToken);
            payment.Order = order;
            return payment;
        }

        private async Task SetTerminalPaymentStatusAsync(
            Payment payment,
            string status,
            string externalTransactionId,
            string responseData,
            string observation,
            CancellationToken cancellationToken)
        {
            var order = payment.Order
                ?? throw new InvalidOperationException("El pago no tiene una orden asociada.");
            var changeRequest = order.FlightChangeRequest
                ?? throw new InvalidOperationException("La orden no tiene una solicitud de cambio asociada.");
            var previousStatus = changeRequest.Status;
            var changeDate = DateTime.UtcNow;

            await using var transaction = await _context.Database
                .BeginTransactionAsync(cancellationToken);

            try
            {
                payment.Status = status;
                payment.ConfirmationDate = changeDate;
                payment.ResponseMessage = LimitLength(responseData, 1000);
                order.Status = status;
                changeRequest.Status = status;

                var transactionExists = await _context.TransactionHistories
                    .AsNoTracking()
                    .AnyAsync(
                        item => item.ExternalTransactionId == externalTransactionId,
                        cancellationToken);

                if (!transactionExists)
                {
                    _context.TransactionHistories.Add(new TransactionHistory
                    {
                        PaymentId = payment.PaymentId,
                        ExternalTransactionId = externalTransactionId,
                        TransactionDate = changeDate,
                        Status = status,
                        Amount = payment.Amount,
                        Gateway = "PayPal",
                        ResponseData = responseData
                    });
                }

                _context.FlightChangeHistories.Add(new FlightChangeHistory
                {
                    FlightChangeRequestId = changeRequest.FlightChangeRequestId,
                    PreviousStatus = previousStatus,
                    NewStatus = status,
                    ChangeDate = changeDate,
                    ChangedBy = payment.UserId,
                    Observation = LimitLength(observation, 500)
                });

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }

        private static string LimitLength(string value, int maximumLength) =>
            value.Length <= maximumLength
                ? value
                : value[..maximumLength];
    }
}
