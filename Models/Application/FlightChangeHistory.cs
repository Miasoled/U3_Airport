using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace U3_Examen_Airport.Models.Application;

[Table("flight_change_history")]
public class FlightChangeHistory
{
    [Key]
    [Column("flight_change_history_id")]
    public int FlightChangeHistoryId { get; set; }

    [Required]
    [Column("flight_change_request_id")]
    public int FlightChangeRequestId { get; set; }

    [Required]
    [Column("previous_status")]
    [StringLength(20)]
    public string PreviousStatus { get; set; } = string.Empty;

    [Required]
    [Column("new_status")]
    [StringLength(20)]
    public string NewStatus { get; set; } = string.Empty;

    [Required]
    [Column("change_date")]
    public DateTime ChangeDate { get; set; } = DateTime.UtcNow;

    [Column("changed_by")]
    [StringLength(450)]
    public string? ChangedBy { get; set; }

    [Column("observation")]
    [StringLength(500)]
    public string? Observation { get; set; }

    [ForeignKey(nameof(FlightChangeRequestId))]
    public FlightChangeRequest? FlightChangeRequest { get; set; }
}