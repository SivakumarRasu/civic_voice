using System.ComponentModel.DataAnnotations;
using CivicVoice.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace CivicVoice.Application.DTOs;

/// <summary>
/// DTO for creating a new grievance. Only contains fields the citizen should provide.
/// Server-side code sets CitizenId, Status, CreatedAt, DueAt, and department assignment.
/// </summary>
public class CreateGrievanceDto
{
    [Required(ErrorMessage = "Please select a ward.")]
    public int WardId { get; set; }

    [Required(ErrorMessage = "Please select a category.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Please describe your complaint.")]
    [StringLength(2000, MinimumLength = 10,
        ErrorMessage = "Description must be between 10 and 2000 characters.")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Optional complaint photo (validated for type and size in the controller).
    /// </summary>
    public IFormFile? Photo { get; set; }

    public bool IsAnonymous { get; set; }
}

/// <summary>
/// Search/filter criteria for grievance queries.
/// </summary>
public class GrievanceSearchDto
{
    public string? SearchTerm { get; set; }
    public GrievanceStatus? Status { get; set; }
    public int? WardId { get; set; }
    public int? CategoryId { get; set; }
    public int? DepartmentId { get; set; }
    public string? CitizenId { get; set; }
}

/// <summary>
/// Department-level report data.
/// </summary>
public class DepartmentReportDto
{
    public List<DepartmentReportItem> Items { get; set; } = new();
}

public class DepartmentReportItem
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalGrievances { get; set; }
    public int OpenGrievances { get; set; }
    public int ResolvedGrievances { get; set; }
    public int SlaBreach { get; set; }
}
