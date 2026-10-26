using System.Diagnostics;
using CivicVoice.Application.Interfaces;
using CivicVoice.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CivicVoice.Web.Controllers;

public class HomeController : Controller
{
    private readonly IGrievanceService _grievanceService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IGrievanceService grievanceService, ILogger<HomeController> logger)
    {
        _grievanceService = grievanceService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // Real database statistics
        var stats = await _grievanceService.GetDashboardStatsAsync(ct);
        var recent = (await _grievanceService.SearchGrievancesAsync(new Application.DTOs.GrievanceSearchDto(), ct))
            .Take(5)
            .ToList();

        ViewBag.Stats = stats;
        ViewBag.RecentGrievances = recent;

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
