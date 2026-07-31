using System;
using System.Collections.Generic;
using NpgsqlTypes;

namespace U3_Examen_Airport.Models;

/// <summary>
/// Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0
/// </summary>
public partial class AirportGeo
{
    public int AirportId { get; set; }

    public string Name { get; set; } = null!;

    public string? City { get; set; }

    public string? Country { get; set; }

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public NpgsqlPoint Geolocation { get; set; }

    public virtual Airport Airport { get; set; } = null!;
}
