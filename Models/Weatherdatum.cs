using System;
using System.Collections.Generic;

namespace U3_Examen_Airport.Models;

/// <summary>
/// Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0
/// </summary>
public partial class Weatherdatum
{
    public DateOnly LogDate { get; set; }

    public TimeOnly Time { get; set; }

    public int Station { get; set; }

    public decimal Temp { get; set; }

    public decimal Humidity { get; set; }

    public decimal Airpressure { get; set; }

    public decimal Wind { get; set; }

    public short Winddirection { get; set; }
}
