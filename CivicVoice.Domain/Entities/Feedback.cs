using System.ComponentModel.DataAnnotations;

namespace CivicVoice.Domain.Entities;

/// <summary>
/// Citizen feedback for a resolved grievance.
/// Enforces at most one feedback per citizen per grievance via a unique composite index.
/// Only accepted when the grievance is resolved and belongs to the submitting citizen.
/// </summary>
public class Feedback
{
    public int FeedbackId { get; set; }

    // Foreign key
    public int GrievanceId { get; set; }

    /// <summary>
    /// The authenticated citizen who submitted this feedback.
    /// </summary>
    [Required]
    [StringLength(450)]
    public string CitizenId { get; set; } = string.Empty;

    /// <summary>
    /// Satisfaction rating from 1 (poor) to 5 (excellent).
    /// </summary>
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }

    [StringLength(1000, ErrorMessage = "Comments cannot exceed 1000 characters.")]
    public string? Comments { get; set; }

    /// <summary>
    /// UTC timestamp when the feedback was submitted.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Grievance Grievance { get; set; } = null!;
}
