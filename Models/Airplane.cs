using System;
using System.Collections.Generic;

namespace U3_Examen_Airport.Models;

/// <summary>
/// Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0
/// </summary>
public partial class Airplane
{
    public int AirplaneId { get; set; }

    public int Capacity { get; set; }

    public int TypeId { get; set; }

    public int AirlineId { get; set; }

    public virtual AirplaneType Type { get; set; } = null!;
}
