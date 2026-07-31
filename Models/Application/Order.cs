using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace U3_Examen_Airport.Models.Application;

[Table("orders")]
public class Order
{
    [Key]
    [Column("order_id")]
    public int OrderId { get; set; }

    [Required]
    [Column("user_id")]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [Column("flight_change_request_id")]
    public int FlightChangeRequestId { get; set; }

    [Required]
    [Column("creation_date")]
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = "Pendiente";

    [Column("subtotal", TypeName = "numeric(10,2)")]
    [Range(0.01, 99999999)]
    public decimal Subtotal { get; set; }

    [Column("penalty_amount", TypeName = "numeric(10,2)")]
    [Range(0, 99999999)]
    public decimal PenaltyAmount { get; set; }

    [Column("total_amount", TypeName = "numeric(10,2)")]
    [Range(0.01, 99999999)]
    public decimal TotalAmount { get; set; }

    [Required]
    [Column("currency")]
    [StringLength(3)]
    public string Currency { get; set; } = "USD";

    [ForeignKey(nameof(FlightChangeRequestId))]
    public FlightChangeRequest? FlightChangeRequest { get; set; }

    public ICollection<OrderDetail> OrderDetails { get; set; }
        = new List<OrderDetail>();

    public ICollection<Payment> Payments { get; set; }
        = new List<Payment>();
}