using System.ComponentModel.DataAnnotations;
using CivicVoice.Domain.Enums;

namespace CivicVoice.Domain.Entities;

/// <summary>
/// The central entity — represents a citizen's municipal complaint.
/// Properties like CitizenId, Status, CreatedAt, DueAt are set server-side,
/// never accepted directly from user input.
/// </summary>
public class Grievance
{
    public int GrievanceId { get; set; }

    /// <summary>
    /// The authenticated citizen who filed this grievance (ASP.NET Identity UserId).
    /// </summary>
    [Required]
    [StringLength(450)]
    public string CitizenId { get; set; } = string.Empty;

    // Foreign keys
    public int WardId { get; set; }
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Please describe your complaint.")]
    [StringLength(2000, MinimumLength = 10,
        ErrorMessage = "Description must be between 10 and 2000 characters.")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Server-generated filename for the uploaded complaint photo. Null if no photo.
    /// </summary>
    [StringLength(500)]
    public string? PhotoPath { get; set; }

    /// <summary>
    /// When true, the citizen's identity is hidden from department officers.
    /// </summary>
    public bool IsAnonymous { get; set; }

    /// <summary>
    /// UTC timestamp when the grievance was created. Set by the server.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// UTC SLA deadline = CreatedAt + Category.DefaultSlaHours. Set by the server.
    /// </summary>
    public DateTime DueAt { get; set; }

    /// <summary>
    /// Current lifecycle status. Changed only through validated state transitions.
    /// </summary>
    public GrievanceStatus Status { get; set; }

    // Navigation
    public Ward Ward { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    public ICollection<StatusHistory> StatusHistories { get; set; } = new List<StatusHistory>();
    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}
