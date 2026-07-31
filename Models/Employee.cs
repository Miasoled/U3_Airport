using System;
using System.Collections.Generic;

namespace U3_Examen_Airport.Models;

/// <summary>
/// Flughafen DB by Stefan Pröll, Eva Zangerle, Wolfgang Gassler is licensed under CC BY 4.0. To view a copy of this license, visit https://creativecommons.org/licenses/by/4.0
/// </summary>
public partial class Employee
{
    public int EmployeeId { get; set; }

    public string Firstname { get; set; } = null!;

    public string Lastname { get; set; } = null!;

    public DateOnly Birthdate { get; set; }

    public char? Sex { get; set; }

    public string Street { get; set; } = null!;

    public string City { get; set; } = null!;

    public short Zip { get; set; }

    public string Country { get; set; } = null!;

    public string? Emailaddress { get; set; }

    public string? Telephoneno { get; set; }

    public decimal? Salary { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }
}
