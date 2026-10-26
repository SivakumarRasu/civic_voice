using System.ComponentModel.DataAnnotations;

namespace CivicVoice.Domain.Entities;

/// <summary>
/// Represents a municipal department responsible for handling specific categories of grievances.
/// Departments are assigned to grievances through the Assignment entity.
/// </summary>
public class Department
{
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "Department name is required.")]
    [StringLength(150, ErrorMessage = "Department name cannot exceed 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    // Navigation
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
