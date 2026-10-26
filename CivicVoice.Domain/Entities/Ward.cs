using System.ComponentModel.DataAnnotations;

namespace CivicVoice.Domain.Entities;

/// <summary>
/// Represents a municipal ward — the geographical division of the city.
/// Each grievance is associated with a ward.
/// </summary>
public class Ward
{
    public int WardId { get; set; }

    [Required(ErrorMessage = "Ward name is required.")]
    [StringLength(100, ErrorMessage = "Ward name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Area name cannot exceed 200 characters.")]
    public string? Area { get; set; }

    [RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode must be a 6-digit number.")]
    public string? Pincode { get; set; }

    // Navigation
    public ICollection<Grievance> Grievances { get; set; } = new List<Grievance>();
}
