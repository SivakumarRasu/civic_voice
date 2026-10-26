using CivicVoice.Domain.Enums;

namespace CivicVoice.Domain.Entities;

/// <summary>
/// Immutable audit record of every status change on a grievance.
/// The initial "Submitted" record is created by the application.
/// Subsequent status changes are recorded by the SQL trigger.
/// </summary>
public class StatusHistory
{
    public int StatusHistoryId { get; set; }

    // Foreign key
    public int GrievanceId { get; set; }

    /// <summary>
    /// Previous status. Null for the initial record (grievance creation).
    /// </summary>
    public GrievanceStatus? OldStatus { get; set; }

    /// <summary>
    /// The new status after the transition.
    /// </summary>
    public GrievanceStatus NewStatus { get; set; }

    /// <summary>
    /// UTC timestamp of the status change.
    /// </summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>
    /// Identifier of the user who made the change. Null when set by system/trigger.
    /// </summary>
    public string? ChangedBy { get; set; }

    // Navigation
    public Grievance Grievance { get; set; } = null!;
}
