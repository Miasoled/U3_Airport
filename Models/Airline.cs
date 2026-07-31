using System;
using System.Collections.Generic;

namespace U3_Examen_Airport.Models;

/// <summary>
/// Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0
/// </summary>
public partial class Airline
{
    public int AirlineId { get; set; }

    public string Iata { get; set; } = null!;

    public string? Airlinename { get; set; }

    public short BaseAirport { get; set; }
}
