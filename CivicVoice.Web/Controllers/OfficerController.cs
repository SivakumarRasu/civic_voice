using CivicVoice.Application.Interfaces;
using CivicVoice.Domain.Enums;
using CivicVoice.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicVoice.Web.Controllers;

[Authorize(Roles = "DepartmentOfficer,Commissioner")]
public class OfficerController : Controller
{
    private readonly IGrievanceService _grievanceService;
    private readonly IDepartmentService _departmentService;

    public OfficerController(IGrievanceService grievanceService, IDepartmentService departmentService)
    {
        _grievanceService = grievanceService;
        _departmentService = departmentService;
    }

    public async Task<IActionResult> Dashboard(int? departmentId, GrievanceStatus? status, CancellationToken ct)
    {
        // Default to Department 2 (Roads) or Department 1 (Water) if none specified
        var targetDeptId = departmentId ?? 2;
        var dept = await _departmentService.GetByIdAsync(targetDeptId, ct);

        var grievances = await _grievanceService.GetGrievancesByDepartmentAsync(targetDeptId, ct);
        if (status.HasValue)
        {
            grievances = grievances.Where(g => g.Status == status.Value);
        }

        var list = grievances.ToList();

        var vm = new OfficerDashboardViewModel
        {
            DepartmentId = targetDeptId,
            DepartmentName = dept?.Name ?? $"Department #{targetDeptId}",
            Grievances = list,
            SelectedStatus = status,
            AssignedCount = list.Count(g => g.Status == GrievanceStatus.Assigned),
            InProgressCount = list.Count(g => g.Status == GrievanceStatus.InProgress),
            ResolvedCount = list.Count(g => g.Status == GrievanceStatus.Resolved),
            BreachedCount = list.Count(g => g.Status != GrievanceStatus.Resolved 
                                         && g.Status != GrievanceStatus.Rejected 
                                         && g.DueAt < DateTime.UtcNow)
        };

        var allDepts = await _departmentService.GetAllDepartmentsAsync(ct);
        ViewBag.Departments = allDepts;

        return View(vm);
    }
}
