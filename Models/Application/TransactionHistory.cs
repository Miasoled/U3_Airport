using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace U3_Examen_Airport.Models.Application;

[Table("transaction_history")]
public class TransactionHistory
{
    [Key]
    [Column("transaction_history_id")]
    public int TransactionHistoryId { get; set; }

    [Required]
    [Column("payment_id")]
    public int PaymentId { get; set; }

    [Required]
    [Column("external_transaction_id")]
    [StringLength(150)]
    public string ExternalTransactionId { get; set; } = string.Empty;

    [Required]
    [Column("transaction_date")]
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = string.Empty;

    [Column("amount", TypeName = "numeric(10,2)")]
    public decimal Amount { get; set; }

    [Required]
    [Column("gateway")]
    [StringLength(30)]
    public string Gateway { get; set; } = "PayPal";

    [Column("response_data", TypeName = "text")]
    public string? ResponseData { get; set; }

    [ForeignKey(nameof(PaymentId))]
    public Payment? Payment { get; set; }
}