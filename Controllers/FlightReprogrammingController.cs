using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using U3_Examen_Airport.Data;
using U3_Examen_Airport.Models;

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
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        int bookingId,
        string passportNumber,
        CancellationToken cancellationToken)
    {
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
    public async Task<IActionResult> SelectFlight(
        int bookingId,
        CancellationToken cancellationToken)
    {
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

        var alternativeFlights = await _airportContext.Flights
            .AsNoTracking()
            .Where(f =>
                f.FlightId != booking.FlightId
                && f.From == booking.Flight.From
                && f.To == booking.Flight.To
                && f.Departure > booking.Flight.Departure)
            .OrderBy(f => f.Departure)
            .Take(20)
            .ToListAsync(cancellationToken);

        ViewBag.Booking = booking;

        return View(alternativeFlights);
    }
}