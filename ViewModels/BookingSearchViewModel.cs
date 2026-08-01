using System.ComponentModel.DataAnnotations;
using U3_Examen_Airport.Models;

namespace U3_Examen_Airport.ViewModels;

public class BookingSearchViewModel
{
    [Display(Name = "Número de reserva")]
    [Range(1, int.MaxValue, ErrorMessage = "Ingrese un número de reserva válido.")]
    public int? BookingId { get; set; }

    [Display(Name = "Número de pasaporte")]
    [Required(ErrorMessage = "Ingrese el número de pasaporte.")]
    [StringLength(9, ErrorMessage = "El número de pasaporte no puede superar 9 caracteres.")]
    public string? PassportNumber { get; set; }

    public string? ErrorMessage { get; set; }
    public List<Booking> UserBookings { get; set; } = [];
    public Dictionary<int, string> AirportNames { get; set; } = [];
    public Booking? SelectedBooking { get; set; }
    public Passenger? SelectedPassenger { get; set; }
    public Airport? OriginAirport { get; set; }
    public Airport? DestinationAirport { get; set; }
    public bool PassengerNotLinked { get; set; }
    public int? LinkedPassengerId { get; set; }
    public string? SearchText { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string SortOrder { get; set; } = "booking_desc";
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; } = 1;
}
