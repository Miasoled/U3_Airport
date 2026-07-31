using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace U3_Examen_Airport.Models.Application;

[Table("payments")]
public class Payment
{
    [Key]
    [Column("payment_id")]
    public int PaymentId { get; set; }

    [Required]
    [Column("order_id")]
    public int OrderId { get; set; }

    [Required]
    [Column("user_id")]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [Column("gateway")]
    [StringLength(30)]
    public string Gateway { get; set; } = "PayPal";

    [Column("external_transaction_id")]
    [StringLength(150)]
    public string? ExternalTransactionId { get; set; }

    [Column("amount", TypeName = "numeric(10,2)")]
    [Range(0.01, 99999999)]
    public decimal Amount { get; set; }

    [Required]
    [Column("currency")]
    [StringLength(3)]
    public string Currency { get; set; } = "USD";

    [Required]
    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = "Pendiente";

    [Required]
    [Column("creation_date")]
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;

    [Column("confirmation_date")]
    public DateTime? ConfirmationDate { get; set; }

    [Column("response_message")]
    [StringLength(1000)]
    public string? ResponseMessage { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order? Order { get; set; }

    public ICollection<TransactionHistory> Transactions { get; set; }
        = new List<TransactionHistory>();
}