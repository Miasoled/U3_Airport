using U3_Examen_Airport.Models;

namespace U3_Examen_Airport.ViewModels;

public class FlightComparisonViewModel
{
    public Booking Booking { get; set; } = null!;
    public Flight CurrentFlight { get; set; } = null!;
    public Flight NewFlight { get; set; } = null!;
    public Airport OriginAirport { get; set; } = null!;
    public Airport DestinationAirport { get; set; } = null!;
    public string NewSeat { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal NewPrice { get; set; }
    public decimal FareDifference { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal TotalAmount { get; set; }
}
