using System;
using System.Collections.Generic;

namespace U3_Examen_Airport.Models;

/// <summary>
/// Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0
/// </summary>
public partial class Flight
{
    public int FlightId { get; set; }

    public string Flightno { get; set; } = null!;

    public short From { get; set; }

    public short To { get; set; }

    public DateTime Departure { get; set; }

    public DateTime Arrival { get; set; }

    public short AirlineId { get; set; }

    public int AirplaneId { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
