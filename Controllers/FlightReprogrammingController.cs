using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using U3_Examen_Airport.Data;
using U3_Examen_Airport.Models;
using U3_Examen_Airport.Models.Application;

namespace U3_Examen_Airport.Controllers;

[Authorize]
public class FlightReprogrammingController : Controller
{
    private readonly AirportContext _airportContext;
    private readonly ApplicationDbContext _applicationContext;

    public FlightReprogrammingController(
        AirportContext airportContext,
        ApplicationDbContext applicationContext)
    {
        _airportContext = airportContext;
        _applicationContext = applicationContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchText,
        DateTime? dateFrom,
        DateTime? dateTo,
        string sortOrder = "booking_desc",
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var userEmail = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return Challenge();
        }

        await PopulateUserBookingsAsync(
            userEmail,
            searchText,
            dateFrom,
            dateTo,
            sortOrder,
            page,
            pageSize,
            cancellationToken);

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        int bookingId,
        string passportNumber,
        CancellationToken cancellationToken)
    {
        var userEmail = User.Identity?.Name;

        if (!string.IsNullOrWhiteSpace(userEmail))
        {
            await PopulateUserBookingsAsync(
                userEmail,
                null,
                null,
                null,
                "booking_desc",
                1,
                10,
                cancellationToken);
        }

        if (bookingId <= 0)
        {
            ViewBag.ErrorMessage =
                "Ingrese un número de reserva válido.";

            return View();
        }

        if (string.IsNullOrWhiteSpace(passportNumber))
        {
            ViewBag.ErrorMessage =
                "Ingrese el número de pasaporte.";

            return View();
        }

        passportNumber = passportNumber.Trim();

        var booking = await _airportContext.Bookings
            .AsNoTracking()
            .Include(b => b.Flight)
            .FirstOrDefaultAsync(
                b => b.BookingId == bookingId,
                cancellationToken);

        if (booking is null)
        {
            ViewBag.ErrorMessage =
                "No se encontró la reserva ingresada.";

            return View();
        }

        var passenger = await _airportContext.Passengers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.PassengerId == booking.PassengerId
                     && p.Passportno == passportNumber,
                cancellationToken);

        if (passenger is null)
        {
            ViewBag.ErrorMessage =
                "El pasaporte no corresponde a la reserva ingresada.";

            return View();
        }

        if (booking.Flight is null)
        {
            ViewBag.ErrorMessage =
                "La reserva no tiene un vuelo asociado.";

            return View();
        }

        var originAirport = await _airportContext.Airports
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.AirportId == booking.Flight.From,
                cancellationToken);

        var destinationAirport = await _airportContext.Airports
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.AirportId == booking.Flight.To,
                cancellationToken);

        ViewBag.Booking = booking;
        ViewBag.Passenger = passenger;
        ViewBag.OriginAirport = originAirport;
        ViewBag.DestinationAirport = destinationAirport;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> History(
        int? bookingId,
        string? status,
        DateTime? dateFrom,
        DateTime? dateTo,
        string sortOrder = "date_desc",
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        page = Math.Max(1, page);
        pageSize = pageSize is 5 or 10 or 20 ? pageSize : 10;
        sortOrder = sortOrder is "date_asc" or "date_desc"
            or "amount_asc" or "amount_desc"
            ? sortOrder
            : "date_desc";

        var allowedStatuses = new[]
        {
            "Pendiente",
            "Aprobado",
            "Cancelado",
            "Rechazado",
            "Fallido"
        };

        status = allowedStatuses.Contains(status) ? status : null;

        var userQuery = _applicationContext.FlightChangeRequests
            .AsNoTracking()
            .Where(request => request.UserId == userId);

        var approvedTotal = await userQuery
            .Where(request => request.Status == "Aprobado")
            .SumAsync(
                request => (decimal?)request.TotalAmount,
                cancellationToken) ?? 0m;

        var historyQuery = userQuery;

        if (bookingId.HasValue && bookingId.Value > 0)
        {
            historyQuery = historyQuery.Where(
                request => request.BookingId == bookingId.Value);
        }

        if (status is not null)
        {
            historyQuery = historyQuery.Where(
                request => request.Status == status);
        }

        if (dateFrom.HasValue)
        {
            historyQuery = historyQuery.Where(
                request => request.RequestDate >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            var exclusiveDateTo = dateTo.Value.Date.AddDays(1);
            historyQuery = historyQuery.Where(
                request => request.RequestDate < exclusiveDateTo);
        }

        var totalRecords = await historyQuery.CountAsync(cancellationToken);
        var totalPages = Math.Max(
            1,
            (int)Math.Ceiling(totalRecords / (double)pageSize));
        page = Math.Min(page, totalPages);

        historyQuery = sortOrder switch
        {
            "date_asc" => historyQuery.OrderBy(request => request.RequestDate),
            "amount_asc" => historyQuery.OrderBy(request => request.TotalAmount),
            "amount_desc" => historyQuery.OrderByDescending(request => request.TotalAmount),
            _ => historyQuery.OrderByDescending(request => request.RequestDate)
        };

        var requests = await historyQuery
            .Include(request => request.Orders)
                .ThenInclude(order => order.Payments)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        ViewBag.BookingId = bookingId;
        ViewBag.Status = status;
        ViewBag.DateFrom = dateFrom;
        ViewBag.DateTo = dateTo;
        ViewBag.SortOrder = sortOrder;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalRecords = totalRecords;
        ViewBag.TotalPages = totalPages;
        ViewBag.ApprovedTotal = approvedTotal;

        return View(requests);
    }

    [HttpGet]
    public async Task<IActionResult> SelectFlight(
        int bookingId,
        CancellationToken cancellationToken)
    {
        if (bookingId <= 0)
        {
            return BadRequest();
        }

        var booking = await _airportContext.Bookings
            .AsNoTracking()
            .Include(b => b.Flight)
            .FirstOrDefaultAsync(
                b => b.BookingId == bookingId,
                cancellationToken);

        if (booking is null || booking.Flight is null)
        {
            return NotFound();
        }

        if (!await CanAccessBookingAsync(booking, cancellationToken))
        {
            return Forbid();
        }

        var alternativeFlights = await _airportContext.Flights
            .AsNoTracking()
            .Where(f =>
                f.FlightId != booking.FlightId
                && f.From == booking.Flight.From
                && f.To == booking.Flight.To)
            .OrderBy(f => f.Departure)
            .Take(20)
            .ToListAsync(cancellationToken);

        var airportIds = new[]
        {
            (int)booking.Flight.From,
            (int)booking.Flight.To
        };

        var airports = await _airportContext.Airports
            .AsNoTracking()
            .Where(a => airportIds.Contains(a.AirportId))
            .ToDictionaryAsync(a => a.AirportId, cancellationToken);

        ViewBag.Booking = booking;
        ViewBag.OriginAirportName = airports.TryGetValue(
            booking.Flight.From,
            out var originAirport)
            ? originAirport.Name
            : "Sin información";
        ViewBag.DestinationAirportName = airports.TryGetValue(
            booking.Flight.To,
            out var destinationAirport)
            ? destinationAirport.Name
            : "Sin información";

        return View(alternativeFlights);
    }

    [HttpGet]
    public async Task<IActionResult> SelectSeat(
        int bookingId,
        int newFlightId,
        CancellationToken cancellationToken)
    {
        var booking = await _airportContext.Bookings
            .AsNoTracking()
            .Include(item => item.Flight)
            .FirstOrDefaultAsync(item => item.BookingId == bookingId, cancellationToken);

        if (booking?.Flight is null
            || !await CanAccessBookingAsync(booking, cancellationToken))
        {
            return booking is null ? NotFound() : Forbid();
        }

        var newFlight = await _airportContext.Flights
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.FlightId == newFlightId
                        && item.FlightId != booking.FlightId
                        && item.From == booking.Flight.From
                        && item.To == booking.Flight.To,
                cancellationToken);

        if (newFlight is null)
        {
            return NotFound();
        }

        ViewBag.Booking = booking;
        ViewBag.AvailableSeats = await GetAvailableSeatsAsync(
            newFlightId,
            cancellationToken);

        return View(newFlight);
    }

    [HttpGet]
    public async Task<IActionResult> Compare(
        int bookingId,
        int newFlightId,
        string newSeat,
        CancellationToken cancellationToken)
    {
        if (bookingId <= 0 || newFlightId <= 0 || string.IsNullOrWhiteSpace(newSeat))
        {
            return NotFound();
        }

        var booking = await _airportContext.Bookings
            .AsNoTracking()
            .Include(b => b.Flight)
            .FirstOrDefaultAsync(
                b => b.BookingId == bookingId,
                cancellationToken);

        if (booking?.Flight is null)
        {
            return NotFound();
        }

        if (!await CanAccessBookingAsync(booking, cancellationToken))
        {
            return Forbid();
        }

        var comparisonLoaded = await LoadComparisonAsync(
            booking,
            newFlightId,
            cancellationToken);

        if (!comparisonLoaded)
        {
            return NotFound();
        }

        var normalizedSeat = NormalizeSeat(newSeat);
        if (!await IsSeatAvailableAsync(newFlightId, normalizedSeat, cancellationToken))
        {
            TempData["ErrorMessage"] =
                "El asiento seleccionado ya no está disponible. Seleccione otro asiento.";
            return RedirectToAction(
                nameof(SelectSeat),
                new { bookingId, newFlightId });
        }

        ViewBag.NewSeat = normalizedSeat;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> ConfirmReprogramming(
        int bookingId,
        int newFlightId,
        string newSeat,
        CancellationToken cancellationToken)
    {
        if (bookingId <= 0 || newFlightId <= 0 || string.IsNullOrWhiteSpace(newSeat))
        {
            return NotFound();
        }

        var booking = await _airportContext.Bookings
            .AsNoTracking()
            .Include(b => b.Flight)
            .FirstOrDefaultAsync(
                b => b.BookingId == bookingId,
                cancellationToken);

        if (booking?.Flight is null)
        {
            return NotFound();
        }

        if (!await CanAccessBookingAsync(booking, cancellationToken))
        {
            return Forbid();
        }

        var newFlight = await _airportContext.Flights
            .AsNoTracking()
            .FirstOrDefaultAsync(
                f => f.FlightId == newFlightId,
                cancellationToken);

        if (newFlight is null
            || newFlight.FlightId == booking.FlightId
            || newFlight.From != booking.Flight.From
            || newFlight.To != booking.Flight.To)
        {
            return NotFound();
        }

        var normalizedSeat = NormalizeSeat(newSeat);
        if (!await IsSeatAvailableAsync(newFlightId, normalizedSeat, cancellationToken))
        {
            TempData["ErrorMessage"] =
                "El asiento seleccionado ya no está disponible. Seleccione otro asiento.";
            return RedirectToAction(
                nameof(SelectSeat),
                new { bookingId, newFlightId });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var originalPrice = booking.Price;
        var newPrice = originalPrice + 25m;
        var fareDifference = Math.Max(0m, newPrice - originalPrice);
        const decimal penaltyAmount = 20m;
        var totalAmount = fareDifference + penaltyAmount;

        var flightChangeRequest = new FlightChangeRequest
        {
            UserId = userId,
            BookingId = booking.BookingId,
            OriginalFlightId = booking.FlightId,
            NewFlightId = newFlight.FlightId,
            NewSeat = normalizedSeat,
            RequestDate = DateTime.UtcNow,
            OriginalPrice = originalPrice,
            NewPrice = newPrice,
            FareDifference = fareDifference,
            PenaltyAmount = penaltyAmount,
            TotalAmount = totalAmount,
            Status = "Pendiente"
        };

        var order = new Order
        {
            UserId = userId,
            FlightChangeRequest = flightChangeRequest,
            CreationDate = DateTime.UtcNow,
            Status = "Pendiente",
            Subtotal = fareDifference,
            PenaltyAmount = penaltyAmount,
            TotalAmount = totalAmount,
            Currency = "USD"
        };

        var orderDetail = new OrderDetail
        {
            Order = order,
            Description =
                $"Reprogramación del vuelo {booking.Flight.Flightno.Trim()} al vuelo {newFlight.Flightno.Trim()}",
            Quantity = 1,
            UnitPrice = totalAmount,
            Subtotal = totalAmount
        };

        await using var transaction = await _applicationContext.Database
            .BeginTransactionAsync(cancellationToken);

        try
        {
            _applicationContext.FlightChangeRequests.Add(flightChangeRequest);
            _applicationContext.Orders.Add(order);
            _applicationContext.OrderDetails.Add(orderDetail);

            await _applicationContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        return RedirectToAction(
            "SelectGateway",
            "Payments",
            new { orderId = order.OrderId });
    }

    private async Task PopulateUserBookingsAsync(
        string userEmail,
        string? searchText,
        DateTime? dateFrom,
        DateTime? dateTo,
        string sortOrder,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = pageSize is 5 or 10 or 20 ? pageSize : 10;
        sortOrder = sortOrder is "departure_asc" or "departure_desc"
            or "price_asc" or "price_desc" or "booking_desc"
            ? sortOrder
            : "booking_desc";
        searchText = string.IsNullOrWhiteSpace(searchText)
            ? null
            : searchText.Trim();

        var normalizedEmail = userEmail.Trim();
        var passengerDetail = await _airportContext.Passengerdetails
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Emailaddress != null
                     && p.Emailaddress.Trim() == normalizedEmail,
                cancellationToken);

        if (passengerDetail is null)
        {
            ViewBag.UserBookings = new List<Booking>();
            ViewBag.AirportNames = new Dictionary<int, string>();
            ViewBag.LinkedPassengerId = null;
            ViewBag.PassengerNotLinked = true;
            SetBookingPaginationViewBag(
                searchText,
                dateFrom,
                dateTo,
                sortOrder,
                page,
                pageSize,
                0,
                1);
            return;
        }

        var bookingsQuery = _airportContext.Bookings
            .AsNoTracking()
            .Include(b => b.Flight)
            .Where(b => b.PassengerId == passengerDetail.PassengerId);

        if (searchText is not null)
        {
            bookingsQuery = bookingsQuery.Where(
                b => b.Flight.Flightno.Contains(searchText));
        }

        if (dateFrom.HasValue)
        {
            bookingsQuery = bookingsQuery.Where(
                b => b.Flight.Departure >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            var exclusiveDateTo = dateTo.Value.Date.AddDays(1);
            bookingsQuery = bookingsQuery.Where(
                b => b.Flight.Departure < exclusiveDateTo);
        }

        var totalRecords = await bookingsQuery.CountAsync(cancellationToken);
        var totalPages = Math.Max(
            1,
            (int)Math.Ceiling(totalRecords / (double)pageSize));
        page = Math.Min(page, totalPages);

        bookingsQuery = sortOrder switch
        {
            "departure_asc" => bookingsQuery.OrderBy(b => b.Flight.Departure),
            "departure_desc" => bookingsQuery.OrderByDescending(b => b.Flight.Departure),
            "price_asc" => bookingsQuery.OrderBy(b => b.Price),
            "price_desc" => bookingsQuery.OrderByDescending(b => b.Price),
            _ => bookingsQuery.OrderByDescending(b => b.BookingId)
        };

        var userBookings = await bookingsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var airportIds = userBookings
            .Where(b => b.Flight is not null)
            .SelectMany(b => new[]
            {
                (int)b.Flight.From,
                (int)b.Flight.To
            })
            .Distinct()
            .ToArray();

        var airportNames = airportIds.Length == 0
            ? new Dictionary<int, string>()
            : await _airportContext.Airports
                .AsNoTracking()
                .Where(a => airportIds.Contains(a.AirportId))
                .ToDictionaryAsync(
                    a => a.AirportId,
                    a => a.Name,
                    cancellationToken);

        ViewBag.UserBookings = userBookings;
        ViewBag.AirportNames = airportNames;
        ViewBag.LinkedPassengerId = passengerDetail.PassengerId;
        ViewBag.PassengerNotLinked = false;
        SetBookingPaginationViewBag(
            searchText,
            dateFrom,
            dateTo,
            sortOrder,
            page,
            pageSize,
            totalRecords,
            totalPages);
    }

    private void SetBookingPaginationViewBag(
        string? searchText,
        DateTime? dateFrom,
        DateTime? dateTo,
        string sortOrder,
        int page,
        int pageSize,
        int totalRecords,
        int totalPages)
    {
        ViewBag.SearchText = searchText;
        ViewBag.DateFrom = dateFrom;
        ViewBag.DateTo = dateTo;
        ViewBag.SortOrder = sortOrder;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalRecords = totalRecords;
        ViewBag.TotalPages = totalPages;
    }

    private async Task<bool> CanAccessBookingAsync(
        Booking booking,
        CancellationToken cancellationToken)
    {
        if (User.IsInRole("Administrador"))
        {
            return true;
        }

        var userEmail = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return false;
        }

        var normalizedEmail = userEmail.Trim();

        return await _airportContext.Passengerdetails
            .AsNoTracking()
            .AnyAsync(
                p => p.PassengerId == booking.PassengerId
                     && p.Emailaddress != null
                     && p.Emailaddress.Trim() == normalizedEmail,
                cancellationToken);
    }

    private async Task<List<string>> GetAvailableSeatsAsync(
        int flightId,
        CancellationToken cancellationToken)
    {
        var occupiedValues = await _airportContext.Bookings
            .AsNoTracking()
            .Where(item => item.FlightId == flightId && item.Seat != null)
            .Select(item => item.Seat!)
            .ToListAsync(cancellationToken);

        var occupiedSeats = occupiedValues
            .Select(NormalizeSeat)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Enumerable.Range(1, 50)
            .SelectMany(row => new[] { 'A', 'B', 'C', 'D', 'E', 'F' }
                .Select(letter => $"{row}{letter}"))
            .Where(seat => !occupiedSeats.Contains(seat))
            .ToList();
    }

    private async Task<bool> IsSeatAvailableAsync(
        int flightId,
        string seat,
        CancellationToken cancellationToken)
    {
        if (!IsValidSeat(seat))
        {
            return false;
        }

        return !await _airportContext.Bookings
            .AsNoTracking()
            .AnyAsync(
                item => item.FlightId == flightId && item.Seat == seat,
                cancellationToken);
    }

    private static string NormalizeSeat(string seat) =>
        seat.Trim().ToUpperInvariant();

    private static bool IsValidSeat(string seat)
    {
        var normalizedSeat = NormalizeSeat(seat);
        if (normalizedSeat.Length is < 2 or > 3
            || normalizedSeat[^1] is < 'A' or > 'F')
        {
            return false;
        }

        return int.TryParse(normalizedSeat[..^1], out var row)
            && row is >= 1 and <= 50;
    }

    private async Task<bool> LoadComparisonAsync(
        Booking booking,
        int newFlightId,
        CancellationToken cancellationToken)
    {
        if (newFlightId <= 0 || booking.Flight is null)
        {
            return false;
        }

        if (booking.FlightId == newFlightId)
        {
            return false;
        }

        var newFlight = await _airportContext.Flights
            .AsNoTracking()
            .FirstOrDefaultAsync(
                f => f.FlightId == newFlightId
                     && f.From == booking.Flight.From
                     && f.To == booking.Flight.To,
                cancellationToken);

        if (newFlight is null)
        {
            return false;
        }

        var airportIds = new[]
        {
            (int)booking.Flight.From,
            (int)booking.Flight.To
        };

        var airports = await _airportContext.Airports
            .AsNoTracking()
            .Where(a => airportIds.Contains(a.AirportId))
            .ToDictionaryAsync(a => a.AirportId, cancellationToken);

        if (!airports.TryGetValue(booking.Flight.From, out var originAirport)
            || !airports.TryGetValue(booking.Flight.To, out var destinationAirport))
        {
            return false;
        }

        var newPrice = booking.Price + 25m;
        var fareDifference = Math.Max(0m, newPrice - booking.Price);
        const decimal penaltyAmount = 20m;
        var totalAmount = fareDifference + penaltyAmount;

        ViewBag.Booking = booking;
        ViewBag.CurrentFlight = booking.Flight;
        ViewBag.NewFlight = newFlight;
        ViewBag.OriginAirport = originAirport;
        ViewBag.DestinationAirport = destinationAirport;
        ViewBag.NewPrice = newPrice;
        ViewBag.FareDifference = fareDifference;
        ViewBag.PenaltyAmount = penaltyAmount;
        ViewBag.TotalAmount = totalAmount;

        return true;
    }
}
