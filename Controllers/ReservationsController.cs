using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using U3_Examen_Airport.Data;
using U3_Examen_Airport.Models;

namespace U3_Examen_Airport.Controllers;

[Authorize]
public class ReservationsController : Controller
{
    private const int MaximumSearchResults = 100;
    private readonly AirportContext _airportContext;

    public ReservationsController(AirportContext airportContext)
    {
        _airportContext = airportContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        short? origin,
        short? destination,
        DateTime? date,
        CancellationToken cancellationToken)
    {
        await LoadFlightFilterOptionsAsync(cancellationToken);

        ViewBag.Origin = origin;
        ViewBag.Destination = destination;
        ViewBag.Date = date?.ToString("yyyy-MM-dd");
        ViewBag.HasSearched = origin.HasValue || destination.HasValue || date.HasValue;

        if (!origin.HasValue || !destination.HasValue || !date.HasValue)
        {
            if (ViewBag.HasSearched)
            {
                ViewBag.ErrorMessage = "Seleccione origen, destino y fecha para buscar vuelos.";
            }

            return View(new List<Flight>());
        }

        if (origin.Value == destination.Value)
        {
            ViewBag.ErrorMessage = "El origen y el destino deben ser diferentes.";
            return View(new List<Flight>());
        }

        var startDate = date.Value.Date;
        var endDate = startDate.AddDays(1);

        var flights = await _airportContext.Flights
            .AsNoTracking()
            .Where(f => f.From == origin.Value
                        && f.To == destination.Value
                        && f.Departure >= startDate
                        && f.Departure < endDate)
            .OrderBy(f => f.Departure)
            .Take(MaximumSearchResults)
            .ToListAsync(cancellationToken);

        ViewBag.EstimatedPrices = flights.ToDictionary(
            flight => flight.FlightId,
            CalculatePrice);
        ViewBag.ResultLimit = MaximumSearchResults;

        return View(flights);
    }

    [HttpGet]
    public async Task<IActionResult> Confirm(
        int flightId,
        CancellationToken cancellationToken)
    {
        var flight = await _airportContext.Flights
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FlightId == flightId, cancellationToken);

        if (flight is null)
        {
            return NotFound();
        }

        var email = GetAuthenticatedEmail();
        if (email is null)
        {
            TempData["ErrorMessage"] = "El usuario autenticado no tiene un correo válido.";
            return RedirectToAction(nameof(Index));
        }

        var passengerDetail = await FindPassengerDetailByEmailAsync(
            email,
            cancellationToken);

        Passenger? passenger = null;
        if (passengerDetail is not null)
        {
            passenger = await _airportContext.Passengers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.PassengerId == passengerDetail.PassengerId,
                    cancellationToken);

            if (passenger is null)
            {
                TempData["ErrorMessage"] =
                    "El correo está asociado a un detalle sin pasajero. Solicite revisión administrativa.";
                return RedirectToAction(nameof(Index));
            }
        }

        LoadConfirmationData(flight, passenger, email);
        return View(flight);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(
        int flightId,
        string? firstName,
        string? lastName,
        string? passportNumber,
        DateTime? birthDate,
        string? phone,
        string? city,
        string? country,
        CancellationToken cancellationToken)
    {
        var flight = await _airportContext.Flights
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FlightId == flightId, cancellationToken);

        if (flight is null)
        {
            return NotFound();
        }

        var email = GetAuthenticatedEmail();
        if (email is null)
        {
            TempData["ErrorMessage"] = "El usuario autenticado no tiene un correo válido.";
            return RedirectToAction(nameof(Index));
        }

        var price = CalculatePrice(flight);
        if (price <= 0m)
        {
            ModelState.AddModelError(string.Empty, "No fue posible calcular un precio válido.");
        }

        var existingDetail = await FindPassengerDetailByEmailAsync(
            email,
            cancellationToken);

        Passenger? existingPassenger = null;
        if (existingDetail is not null)
        {
            existingPassenger = await _airportContext.Passengers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.PassengerId == existingDetail.PassengerId,
                    cancellationToken);

            if (existingPassenger is null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "El correo está asociado a un detalle sin pasajero.");
            }
        }
        else
        {
            ValidateNewPassenger(
                firstName,
                lastName,
                passportNumber,
                birthDate);
        }

        if (!ModelState.IsValid)
        {
            PreservePassengerForm(firstName, lastName, passportNumber, birthDate, phone, city, country);
            LoadConfirmationData(flight, existingPassenger, email);
            return View(flight);
        }

        await using var transaction = await _airportContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            var currentFlightExists = await _airportContext.Flights
                .AsNoTracking()
                .AnyAsync(item => item.FlightId == flightId, cancellationToken);

            if (!currentFlightExists)
            {
                await transaction.RollbackAsync(cancellationToken);
                return NotFound();
            }

            var normalizedEmail = email.Trim().ToLower();
            var currentDetail = await _airportContext.Passengerdetails
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.Emailaddress != null
                            && item.Emailaddress.Trim().ToLower() == normalizedEmail,
                    cancellationToken);

            int passengerId;
            if (currentDetail is not null)
            {
                var passengerExists = await _airportContext.Passengers
                    .AsNoTracking()
                    .AnyAsync(
                        item => item.PassengerId == currentDetail.PassengerId,
                        cancellationToken);

                if (!passengerExists)
                {
                    throw new InvalidOperationException(
                        "El detalle del pasajero no tiene un pasajero válido.");
                }

                passengerId = currentDetail.PassengerId;
            }
            else
            {
                var normalizedPassport = passportNumber!.Trim().ToLower();
                var passportExists = await _airportContext.Passengers
                    .AsNoTracking()
                    .AnyAsync(
                        item => item.Passportno.Trim().ToLower() == normalizedPassport,
                        cancellationToken);

                if (passportExists)
                {
                    ModelState.AddModelError(
                        nameof(passportNumber),
                        "El número de pasaporte ya pertenece a otro pasajero.");
                    await transaction.RollbackAsync(cancellationToken);
                    PreservePassengerForm(firstName, lastName, passportNumber, birthDate, phone, city, country);
                    LoadConfirmationData(flight, null, email);
                    return View(flight);
                }

                passengerId = (await _airportContext.Passengers
                    .MaxAsync(item => (int?)item.PassengerId, cancellationToken) ?? 0) + 1;

                _airportContext.Passengers.Add(new Passenger
                {
                    PassengerId = passengerId,
                    Firstname = firstName!.Trim(),
                    Lastname = lastName!.Trim(),
                    Passportno = passportNumber!.Trim()
                });

                _airportContext.Passengerdetails.Add(new Passengerdetail
                {
                    PassengerId = passengerId,
                    Birthdate = DateOnly.FromDateTime(birthDate!.Value),
                    Street = "No especificada",
                    City = string.IsNullOrWhiteSpace(city) ? "No especificada" : city.Trim(),
                    Zip = 0,
                    Country = string.IsNullOrWhiteSpace(country) ? "No especificado" : country.Trim(),
                    Emailaddress = email,
                    Telephoneno = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim()
                });

                await _airportContext.SaveChangesAsync(cancellationToken);
            }

            var alreadyBooked = await _airportContext.Bookings
                .AsNoTracking()
                .AnyAsync(
                    item => item.FlightId == flightId
                            && item.PassengerId == passengerId,
                    cancellationToken);

            if (alreadyBooked)
            {
                ModelState.AddModelError(string.Empty, "Ya tiene una reserva para este vuelo.");
                await transaction.RollbackAsync(cancellationToken);
                LoadConfirmationData(flight, existingPassenger, email);
                return View(flight);
            }

            var seat = await FindAvailableSeatAsync(flightId, cancellationToken);
            if (seat is null)
            {
                ModelState.AddModelError(string.Empty, "No existen asientos disponibles para este vuelo.");
                await transaction.RollbackAsync(cancellationToken);
                LoadConfirmationData(flight, existingPassenger, email);
                return View(flight);
            }

            var seatStillAvailable = !await _airportContext.Bookings
                .AsNoTracking()
                .AnyAsync(
                    item => item.FlightId == flightId && item.Seat == seat,
                    cancellationToken);

            if (!seatStillAvailable)
            {
                throw new InvalidOperationException("El asiento seleccionado dejó de estar disponible.");
            }

            var bookingId = (await _airportContext.Bookings
                .MaxAsync(item => (int?)item.BookingId, cancellationToken) ?? 0) + 1;

            _airportContext.Bookings.Add(new Booking
            {
                BookingId = bookingId,
                PassengerId = passengerId,
                FlightId = flightId,
                Seat = seat,
                Price = price
            });

            await _airportContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            TempData["SuccessMessage"] =
                $"Reserva #{bookingId} creada correctamente. Asiento asignado: {seat}.";

            return RedirectToAction("Index", "FlightReprogramming");
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            ModelState.AddModelError(
                string.Empty,
                "No fue posible crear la reserva porque los datos cambiaron. Inténtelo nuevamente.");
        }
        catch (InvalidOperationException exception)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            ModelState.AddModelError(string.Empty, exception.Message);
        }

        PreservePassengerForm(firstName, lastName, passportNumber, birthDate, phone, city, country);
        LoadConfirmationData(flight, existingPassenger, email);
        return View(flight);
    }

    private async Task LoadFlightFilterOptionsAsync(CancellationToken cancellationToken)
    {
        ViewBag.Origins = await _airportContext.Flights
            .AsNoTracking()
            .Select(flight => flight.From)
            .Distinct()
            .OrderBy(id => id)
            .ToListAsync(cancellationToken);

        ViewBag.Destinations = await _airportContext.Flights
            .AsNoTracking()
            .Select(flight => flight.To)
            .Distinct()
            .OrderBy(id => id)
            .ToListAsync(cancellationToken);
    }

    private async Task<Passengerdetail?> FindPassengerDetailByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLower();

        return await _airportContext.Passengerdetails
            .AsNoTracking()
            .FirstOrDefaultAsync(
                detail => detail.Emailaddress != null
                          && detail.Emailaddress.Trim().ToLower() == normalizedEmail,
                cancellationToken);
    }

    private async Task<string?> FindAvailableSeatAsync(
        int flightId,
        CancellationToken cancellationToken)
    {
        var occupiedSeatValues = await _airportContext.Bookings
            .AsNoTracking()
            .Where(booking => booking.FlightId == flightId && booking.Seat != null)
            .Select(booking => booking.Seat!)
            .ToListAsync(cancellationToken);

        var occupiedSeats = occupiedSeatValues
            .Select(seat => seat.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var availableSeats = Enumerable.Range(1, 30)
            .SelectMany(row => new[] { 'A', 'B', 'C', 'D', 'E', 'F' }
                .Select(letter => $"{row}{letter}"))
            .Where(seat => !occupiedSeats.Contains(seat))
            .OrderBy(_ => Random.Shared.Next())
            .ToList();

        return availableSeats.FirstOrDefault();
    }

    private string? GetAuthenticatedEmail()
    {
        var email = User.FindFirstValue(ClaimTypes.Email)
            ?? User.Identity?.Name;

        return string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim();
    }

    private static decimal CalculatePrice(Flight flight)
    {
        var durationHours = Math.Max(0.5, (flight.Arrival - flight.Departure).TotalHours);
        return decimal.Round(75m + ((decimal)durationHours * 15m), 2);
    }

    private void LoadConfirmationData(Flight flight, Passenger? passenger, string email)
    {
        ViewBag.EstimatedPrice = CalculatePrice(flight);
        ViewBag.UserEmail = email;
        ViewBag.ExistingPassenger = passenger;
        ViewBag.RequiresPassengerData = passenger is null;
    }

    private void ValidateNewPassenger(
        string? firstName,
        string? lastName,
        string? passportNumber,
        DateTime? birthDate)
    {
        if (string.IsNullOrWhiteSpace(firstName) || firstName.Trim().Length > 100)
        {
            ModelState.AddModelError(nameof(firstName), "Ingrese un nombre válido de hasta 100 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(lastName) || lastName.Trim().Length > 100)
        {
            ModelState.AddModelError(nameof(lastName), "Ingrese un apellido válido de hasta 100 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(passportNumber)
            || passportNumber.Trim().Length > 9)
        {
            ModelState.AddModelError(nameof(passportNumber), "Ingrese un pasaporte válido de hasta 9 caracteres.");
        }

        if (!birthDate.HasValue || birthDate.Value.Date > DateTime.UtcNow.Date)
        {
            ModelState.AddModelError(nameof(birthDate), "Ingrese una fecha de nacimiento válida.");
        }
    }

    private void PreservePassengerForm(
        string? firstName,
        string? lastName,
        string? passportNumber,
        DateTime? birthDate,
        string? phone,
        string? city,
        string? country)
    {
        ViewBag.FirstName = firstName;
        ViewBag.LastName = lastName;
        ViewBag.PassportNumber = passportNumber;
        ViewBag.BirthDate = birthDate?.ToString("yyyy-MM-dd");
        ViewBag.Phone = phone;
        ViewBag.City = city;
        ViewBag.Country = country;
    }
}
