using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace U3_Examen_Airport.Models.Application;

[Table("order_details")]
public class OrderDetail
{
    [Key]
    [Column("order_detail_id")]
    public int OrderDetailId { get; set; }

    [Required]
    [Column("order_id")]
    public int OrderId { get; set; }

    [Required]
    [Column("description")]
    [StringLength(250)]
    public string Description { get; set; } = string.Empty;

    [Column("quantity")]
    [Range(1, 100)]
    public int Quantity { get; set; } = 1;

    [Column("unit_price", TypeName = "numeric(10,2)")]
    [Range(0, 99999999)]
    public decimal UnitPrice { get; set; }

    [Column("subtotal", TypeName = "numeric(10,2)")]
    [Range(0, 99999999)]
    public decimal Subtotal { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order? Order { get; set; }
}   