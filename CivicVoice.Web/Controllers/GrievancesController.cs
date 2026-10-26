using System.Security.Claims;
using System.Text;
using CivicVoice.Application.DTOs;
using CivicVoice.Application.Interfaces;
using CivicVoice.Domain.Enums;
using CivicVoice.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CivicVoice.Web.Controllers;

public class GrievancesController : Controller
{
    private readonly IGrievanceService _grievanceService;
    private readonly ICategoryService _categoryService;
    private readonly IWardService _wardService;
    private readonly IDepartmentService _departmentService;
    private readonly IFeedbackService _feedbackService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<GrievancesController> _logger;

    public GrievancesController(
        IGrievanceService grievanceService,
        ICategoryService categoryService,
        IWardService wardService,
        IDepartmentService departmentService,
        IFeedbackService feedbackService,
        IWebHostEnvironment env,
        ILogger<GrievancesController> logger)
    {
        _grievanceService = grievanceService;
        _categoryService = categoryService;
        _wardService = wardService;
        _departmentService = departmentService;
        _feedbackService = feedbackService;
        _env = env;
        _logger = logger;
    }

    // ── Conventional & Session-Aware Listing ────────────────────────────────
    public async Task<IActionResult> Index(
        string? searchTerm,
        GrievanceStatus? status,
        int? wardId,
        int? departmentId,
        bool overdueOnly,
        CancellationToken ct)
    {
        // Session: Store harmless ward preference (Phase 8)
        if (wardId.HasValue)
        {
            HttpContext.Session.SetInt32("PreferredWardId", wardId.Value);
        }
        else if (HttpContext.Session.GetInt32("PreferredWardId") is int savedWard)
        {
            wardId = savedWard;
        }

        var searchDto = new GrievanceSearchDto
        {
            SearchTerm = searchTerm,
            Status = status,
            WardId = wardId,
            DepartmentId = departmentId
        };

        var results = await _grievanceService.SearchGrievancesAsync(searchDto, ct);
        if (overdueOnly)
        {
            results = results.Where(g => g.Status != GrievanceStatus.Resolved 
                                      && g.Status != GrievanceStatus.Rejected 
                                      && g.DueAt < DateTime.UtcNow);
        }

        var stats = await _grievanceService.GetDashboardStatsAsync(ct);
        var wards = await _wardService.GetAllWardsAsync(ct);
        var departments = await _departmentService.GetAllDepartmentsAsync(ct);

        var vm = new GrievanceListViewModel
        {
            Grievances = results,
            SearchTerm = searchTerm,
            Status = status,
            WardId = wardId,
            DepartmentId = departmentId,
            OverdueOnly = overdueOnly,
            Wards = wards.Select(w => new SelectListItem(w.Name, w.WardId.ToString(), w.WardId == wardId)).ToList(),
            Departments = departments.Select(d => new SelectListItem(d.Name, d.DepartmentId.ToString(), d.DepartmentId == departmentId)).ToList(),
            TotalCount = stats.TotalGrievances,
            OpenCount = stats.OpenGrievances,
            ResolvedCount = stats.ResolvedGrievances,
            BreachedCount = stats.SlaBreach
        };

        return View(vm);
    }

    // ── Phase 9: Real AJAX Live Search Endpoint ─────────────────────────────
    [HttpGet]
    public async Task<IActionResult> SearchAjax(
        string? searchTerm,
        GrievanceStatus? status,
        int? wardId,
        int? departmentId,
        bool overdueOnly,
        CancellationToken ct)
    {
        var searchDto = new GrievanceSearchDto
        {
            SearchTerm = searchTerm,
            Status = status,
            WardId = wardId,
            DepartmentId = departmentId
        };

        var results = await _grievanceService.SearchGrievancesAsync(searchDto, ct);
        if (overdueOnly)
        {
            results = results.Where(g => g.Status != GrievanceStatus.Resolved 
                                      && g.Status != GrievanceStatus.Rejected 
                                      && g.DueAt < DateTime.UtcNow);
        }

        return PartialView("_GrievanceRows", results);
    }

    // ── Phase 8: Attribute Routing with Route Constraint ────────────────────
    [HttpGet("complaint/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var grievance = await _grievanceService.GetGrievanceByIdAsync(id, ct);
        if (grievance == null)
        {
            return NotFound($"Grievance #{id} does not exist.");
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var isCitizen = User.IsInRole("Citizen") || !User.Identity?.IsAuthenticated == true;
        var isOfficer = User.IsInRole("DepartmentOfficer");
        var isCommissioner = User.IsInRole("Commissioner");

        // Privacy rule: If anonymous, hide citizen ID unless commissioner or self
        if (grievance.IsAnonymous && currentUserId != grievance.CitizenId && !isCommissioner)
        {
            // Anonymous presentation
            ViewBag.DisplayCitizenName = "Anonymous Citizen";
        }
        else
        {
            ViewBag.DisplayCitizenName = grievance.CitizenId;
        }

        var existingFeedback = await _feedbackService.GetFeedbacksForGrievanceAsync(id, ct);
        var userFeedback = existingFeedback.FirstOrDefault(f => f.CitizenId == currentUserId);

        var vm = new GrievanceDetailsViewModel
        {
            Grievance = grievance,
            CanSubmitFeedback = grievance.Status == GrievanceStatus.Resolved 
                                && currentUserId == grievance.CitizenId 
                                && userFeedback == null,
            CanChangeStatus = isOfficer || isCommissioner,
            AllowedNextStatuses = GetAllowedNextStatuses(grievance.Status),
            ExistingFeedbacks = existingFeedback,
            HistoryTimeline = grievance.StatusHistories.OrderBy(h => h.ChangedAt).ToList()
        };

        return View(vm);
    }

    // ── Phase 5 / 7: Grievance Creation ─────────────────────────────────────
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var vm = new CreateGrievanceViewModel();
        await PopulateDropdownsAsync(vm, ct);
        return View(vm);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateGrievanceViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(vm, ct);
            return View(vm);
        }

        var citizenId = User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? User.Identity?.Name 
            ?? "anonymous_citizen";

        var dto = new CreateGrievanceDto
        {
            WardId = vm.WardId,
            CategoryId = vm.CategoryId,
            Description = vm.Description,
            Photo = vm.Photo,
            IsAnonymous = vm.IsAnonymous
        };

        // Handle secure photo upload
        if (vm.Photo != null && vm.Photo.Length > 0)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var extension = Path.GetExtension(vm.Photo.FileName).ToLowerInvariant();
            var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await vm.Photo.CopyToAsync(stream, ct);
            }

            // Stored relative web path
            // Service will attach PhotoPath
        }

        var result = await _grievanceService.CreateGrievanceAsync(dto, citizenId, ct);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to create grievance.");
            await PopulateDropdownsAsync(vm, ct);
            return View(vm);
        }

        TempData["SuccessMessage"] = $"Grievance #{result.Data!.GrievanceId} has been successfully registered and routed to the department.";
        return RedirectToAction(nameof(Details), new { id = result.Data.GrievanceId });
    }

    // ── Status Transition ───────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "DepartmentOfficer,Commissioner")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int grievanceId, GrievanceStatus newStatus, CancellationToken ct)
    {
        var changedBy = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Officer";

        var result = await _grievanceService.UpdateStatusAsync(grievanceId, newStatus, changedBy, ct);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = $"Grievance #{grievanceId} status updated to {newStatus}.";
        }

        return RedirectToAction(nameof(Details), new { id = grievanceId });
    }

    // ── Phase 12: CSV Export with Security and CSV Escaping ─────────────────
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ExportCsv(
        string? searchTerm,
        GrievanceStatus? status,
        int? wardId,
        int? departmentId,
        CancellationToken ct)
    {
        var searchDto = new GrievanceSearchDto
        {
            SearchTerm = searchTerm,
            Status = status,
            WardId = wardId,
            DepartmentId = departmentId
        };

        // Restrict citizens to their own complaints
        if (!User.IsInRole("DepartmentOfficer") && !User.IsInRole("Commissioner"))
        {
            searchDto.CitizenId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        var grievances = await _grievanceService.SearchGrievancesAsync(searchDto, ct);

        var sb = new StringBuilder();
        // CSV Header
        sb.AppendLine("GrievanceId,Ward,Category,Department,Status,CreatedAtUtc,DueAtUtc,IsSlaBreached,Description");

        foreach (var g in grievances)
        {
            var isBreached = g.Status != GrievanceStatus.Resolved 
                          && g.Status != GrievanceStatus.Rejected 
                          && g.DueAt < DateTime.UtcNow;

            var wardName = EscapeCsv(g.Ward?.Name ?? g.WardId.ToString());
            var catName = EscapeCsv(g.Category?.Name ?? g.CategoryId.ToString());
            var deptName = EscapeCsv(g.Category?.Department?.Name ?? "N/A");
            var desc = EscapeCsv(g.Description);

            sb.AppendLine($"{g.GrievanceId},{wardName},{catName},{deptName},{g.Status},{g.CreatedAt:u},{g.DueAt:u},{isBreached},{desc}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"CivicVoice_Report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

        return File(bytes, "text/csv", fileName);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        // Double up existing quotes and enclose in quotes
        return $"\"{value.Replace("\"", "\"\"").Replace("\r\n", " ").Replace("\n", " ")}\"";
    }

    private async Task PopulateDropdownsAsync(CreateGrievanceViewModel vm, CancellationToken ct)
    {
        var wards = await _wardService.GetAllWardsAsync(ct);
        var categories = await _categoryService.GetAllWithDepartmentsAsync(ct);

        vm.Wards = wards.Select(w => new SelectListItem($"{w.Name} ({w.Area})", w.WardId.ToString())).ToList();
        vm.Categories = categories.Select(c => new SelectListItem($"{c.Name} (Dept: {c.Department?.Name ?? "General"}, SLA: {c.DefaultSlaHours}h)", c.CategoryId.ToString())).ToList();
    }

    private static List<GrievanceStatus> GetAllowedNextStatuses(GrievanceStatus current)
    {
        return current switch
        {
            GrievanceStatus.Submitted => new() { GrievanceStatus.Assigned, GrievanceStatus.Rejected },
            GrievanceStatus.Assigned => new() { GrievanceStatus.InProgress, GrievanceStatus.Rejected },
            GrievanceStatus.InProgress => new() { GrievanceStatus.Resolved, GrievanceStatus.Rejected },
            _ => new()
        };
    }
}
