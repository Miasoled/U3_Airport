using System;
using System.Collections.Generic;

namespace U3_Examen_Airport.Models;

/// <summary>
/// Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0
/// </summary>
public partial class FlightLog
{
    public long FlightLogId { get; set; }

    public DateTime LogDate { get; set; }

    public string User { get; set; } = null!;

    public int FlightId { get; set; }

    public string FlightnoOld { get; set; } = null!;

    public string FlightnoNew { get; set; } = null!;

    public short FromOld { get; set; }

    public short ToOld { get; set; }

    public short FromNew { get; set; }

    public short ToNew { get; set; }

    public DateTime DepartureOld { get; set; }

    public DateTime ArrivalOld { get; set; }

    public DateTime DepartureNew { get; set; }

    public DateTime ArrivalNew { get; set; }

    public int AirplaneIdOld { get; set; }

    public int AirplaneIdNew { get; set; }

    public short AirlineIdOld { get; set; }

    public short AirlineIdNew { get; set; }

    public string? Comment { get; set; }
}
