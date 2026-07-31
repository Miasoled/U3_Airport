using System;
using System.Collections.Generic;

namespace U3_Examen_Airport.Models;

/// <summary>
/// Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0
/// </summary>
public partial class Passengerdetail
{
    public int PassengerId { get; set; }

    public DateOnly Birthdate { get; set; }

    public char? Sex { get; set; }

    public string Street { get; set; } = null!;

    public string City { get; set; } = null!;

    public short Zip { get; set; }

    public string Country { get; set; } = null!;

    public string? Emailaddress { get; set; }

    public string? Telephoneno { get; set; }
}
