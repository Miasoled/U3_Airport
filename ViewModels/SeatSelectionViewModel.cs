using System.ComponentModel.DataAnnotations;
using U3_Examen_Airport.Models;

namespace U3_Examen_Airport.ViewModels;

public class SeatSelectionViewModel
{
    public int BookingId { get; set; }
    public int NewFlightId { get; set; }
    public Booking CurrentBooking { get; set; } = null!;
    public Flight CurrentFlight { get; set; } = null!;
    public Flight NewFlight { get; set; } = null!;
    public Airport? OriginAirport { get; set; }
    public Airport? DestinationAirport { get; set; }
    public List<string> AvailableSeats { get; set; } = [];

    [Required(ErrorMessage = "Seleccione un asiento disponible.")]
    public string? SelectedSeat { get; set; }
}
