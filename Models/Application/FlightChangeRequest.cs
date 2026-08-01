using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace U3_Examen_Airport.Models.Application;

[Table("flight_change_requests")]
public class FlightChangeRequest
{
    [Key]
    [Column("flight_change_request_id")]
    public int FlightChangeRequestId { get; set; }

    [Required]
    [Column("user_id")]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [Column("booking_id")]
    public int BookingId { get; set; }

    [Required]
    [Column("original_flight_id")]
    public int OriginalFlightId { get; set; }

    [Required]
    [Column("new_flight_id")]
    public int NewFlightId { get; set; }

    [Column("new_seat")]
    [StringLength(4)]
    public string? NewSeat { get; set; }

    [Required]
    [Column("request_date")]
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;

    [Column("original_price", TypeName = "numeric(10,2)")]
    [Range(0, 99999999)]
    public decimal OriginalPrice { get; set; }

    [Column("new_price", TypeName = "numeric(10,2)")]
    [Range(0, 99999999)]
    public decimal NewPrice { get; set; }

    [Column("fare_difference", TypeName = "numeric(10,2)")]
    public decimal FareDifference { get; set; }

    [Column("penalty_amount", TypeName = "numeric(10,2)")]
    [Range(0, 99999999)]
    public decimal PenaltyAmount { get; set; }

    [Column("total_amount", TypeName = "numeric(10,2)")]
    [Range(0, 99999999)]
    public decimal TotalAmount { get; set; }

    [Required]
    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = "Pendiente";

    [Column("reason")]
    [StringLength(500)]
    public string? Reason { get; set; }

    public ICollection<FlightChangeHistory> Histories { get; set; }
        = new List<FlightChangeHistory>();

    public ICollection<Order> Orders { get; set; }
        = new List<Order>();
}
