using System;
using System.Collections.Generic;

namespace U3_Examen_Airport.Models;

/// <summary>
/// Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0
/// </summary>
public partial class AirportReachable
{
    public int AirportId { get; set; }

    public int? Hops { get; set; }

    public virtual Airport Airport { get; set; } = null!;
}
