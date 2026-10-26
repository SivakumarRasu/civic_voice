using System.ComponentModel.DataAnnotations;

namespace CivicVoice.Domain.Entities;

/// <summary>
/// Represents a grievance category (e.g., Water Leakage, Road Damage).
/// Each category belongs to a department and defines an SLA (Service Level Agreement) deadline.
/// </summary>
public class Category
{
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(150, ErrorMessage = "Category name cannot exceed 150 characters.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Default hours within which a grievance of this category should be resolved.
    /// Used to calculate the DueAt deadline on the grievance.
    /// </summary>
    [Range(1, 720, ErrorMessage = "SLA hours must be between 1 and 720 (30 days).")]
    public int DefaultSlaHours { get; set; }

    // Foreign key
    public int DepartmentId { get; set; }

    // Navigation
    public Department Department { get; set; } = null!;
    public ICollection<Grievance> Grievances { get; set; } = new List<Grievance>();
}
