using U3_Examen_Airport.Models;

namespace U3_Examen_Airport.ViewModels;

public class FlightSelectionViewModel
{
    public Booking CurrentBooking { get; set; } = null!;
    public Flight CurrentFlight { get; set; } = null!;
    public Airport? OriginAirport { get; set; }
    public Airport? DestinationAirport { get; set; }
    public List<Flight> AlternativeFlights { get; set; } = [];
    public Dictionary<int, string> AirportNames { get; set; } = [];
}
