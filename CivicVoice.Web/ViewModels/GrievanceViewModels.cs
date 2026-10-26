using System.ComponentModel.DataAnnotations;
using CivicVoice.Domain.Entities;
using CivicVoice.Domain.Enums;
using CivicVoice.Web.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CivicVoice.Web.ViewModels;

/// <summary>
/// View model for submitting a grievance.
/// Demonstrates:
/// - DataAnnotations ([Required], [StringLength], [Display])
/// - Remote validation ([Remote]) for server-side ward check
/// - Custom validation attribute ([NotFutureDate])
/// - IValidatableObject for cross-field business validation
/// </summary>
public class CreateGrievanceViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Please select a municipal ward.")]
    [Display(Name = "Municipal Ward")]
    [Remote(action: "ValidateWard", controller: "Validation", ErrorMessage = "Selected ward does not exist in our municipal records.")]
    public int WardId { get; set; }

    [Required(ErrorMessage = "Please select a grievance category.")]
    [Display(Name = "Grievance Category")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Please provide a detailed description of the grievance.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 2000 characters.")]
    [Display(Name = "Complaint Description")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Optional Photo Evidence")]
    public IFormFile? Photo { get; set; }

    [Display(Name = "Submit Anonymously")]
    public bool IsAnonymous { get; set; }

    [Display(Name = "Incident Date / Time (Optional)")]
    [NotFutureDate(ErrorMessage = "The incident date cannot be set in the future.")]
    [DataType(DataType.DateTime)]
    public DateTime? IncidentDate { get; set; } = DateTime.UtcNow;

    // Dropdown options for Razor TagHelpers
    public List<SelectListItem> Wards { get; set; } = new();
    public List<SelectListItem> Categories { get; set; } = new();

    /// <summary>
    /// Demonstrates IValidatableObject for cross-field validation.
    /// If IncidentDate is provided, it cannot be older than 90 days.
    /// Also validates photo extension and size.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IncidentDate.HasValue && IncidentDate.Value < DateTime.UtcNow.AddDays(-90))
        {
            yield return new ValidationResult(
                "Incident date cannot be older than 90 days.",
                new[] { nameof(IncidentDate) });
        }

        if (Photo != null)
        {
            const long maxFileSize = 5 * 1024 * 1024; // 5 MB
            if (Photo.Length > maxFileSize)
            {
                yield return new ValidationResult(
                    "Photo size exceeds the maximum allowed limit of 5 MB.",
                    new[] { nameof(Photo) });
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(Photo.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                yield return new ValidationResult(
                    "Only JPG, JPEG, PNG, or WebP images are allowed.",
                    new[] { nameof(Photo) });
            }
        }
    }
}

public class GrievanceDetailsViewModel
{
    public Grievance Grievance { get; set; } = null!;
    public bool CanSubmitFeedback { get; set; }
    public bool CanChangeStatus { get; set; }
    public List<GrievanceStatus> AllowedNextStatuses { get; set; } = new();
    public int NewRating { get; set; } = 5;
    public string? FeedbackComments { get; set; }
    public IEnumerable<Feedback> ExistingFeedbacks { get; set; } = new List<Feedback>();
    public IEnumerable<StatusHistory> HistoryTimeline { get; set; } = new List<StatusHistory>();
}

public class GrievanceListViewModel
{
    public IEnumerable<Grievance> Grievances { get; set; } = new List<Grievance>();
    public string? SearchTerm { get; set; }
    public GrievanceStatus? Status { get; set; }
    public int? WardId { get; set; }
    public int? DepartmentId { get; set; }
    public bool OverdueOnly { get; set; }

    public List<SelectListItem> Wards { get; set; } = new();
    public List<SelectListItem> Departments { get; set; } = new();

    public int TotalCount { get; set; }
    public int OpenCount { get; set; }
    public int ResolvedCount { get; set; }
    public int BreachedCount { get; set; }
}

public class OfficerDashboardViewModel
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public IEnumerable<Grievance> Grievances { get; set; } = new List<Grievance>();
    public GrievanceStatus? SelectedStatus { get; set; }
    public int AssignedCount { get; set; }
    public int InProgressCount { get; set; }
    public int ResolvedCount { get; set; }
    public int BreachedCount { get; set; }
}

public class CommissionerDashboardViewModel
{
    public CivicVoice.Application.Interfaces.DashboardStats GlobalStats { get; set; } = new();
    public Application.DTOs.DepartmentReportDto DepartmentReports { get; set; } = new();
    public IEnumerable<Grievance> OverdueGrievances { get; set; } = new List<Grievance>();
}
