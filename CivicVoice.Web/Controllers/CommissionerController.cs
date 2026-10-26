using CivicVoice.Application.Interfaces;
using CivicVoice.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicVoice.Web.Controllers;

[Authorize(Roles = "Commissioner")]
public class CommissionerController : Controller
{
    private readonly IGrievanceService _grievanceService;
    private readonly IDepartmentService _departmentService;

    public CommissionerController(
        IGrievanceService grievanceService,
        IDepartmentService departmentService)
    {
        _grievanceService = grievanceService;
        _departmentService = departmentService;
    }

    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var stats = await _grievanceService.GetDashboardStatsAsync(ct);
        var deptReports = await _departmentService.GetDepartmentReportAsync(ct);
        var overdue = await _grievanceService.GetOverdueGrievancesAsync(ct);

        var vm = new CommissionerDashboardViewModel
        {
            GlobalStats = stats,
            DepartmentReports = deptReports,
            OverdueGrievances = overdue
        };

        return View(vm);
    }
}
