using System.ComponentModel.DataAnnotations;

namespace CivicVoice.Domain.Entities;

/// <summary>
/// Explicit many-to-many join entity between Grievance and Department.
/// Contains business metadata (timestamps, officer assignment).
/// Historical assignments are preserved — reassignment creates a new record.
/// </summary>
public class Assignment
{
    public int AssignmentId { get; set; }

    // Foreign keys
    public int GrievanceId { get; set; }
    public int DepartmentId { get; set; }

    /// <summary>
    /// The department officer assigned to handle this grievance.
    /// Nullable until an officer claims or is assigned the grievance.
    /// </summary>
    [StringLength(450)]
    public string? OfficerId { get; set; }

    /// <summary>
    /// UTC timestamp when this assignment was created.
    /// </summary>
    public DateTime AssignedAt { get; set; }

    // Navigation
    public Grievance Grievance { get; set; } = null!;
    public Department Department { get; set; } = null!;
}
