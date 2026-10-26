using CivicVoice.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CivicVoice.Web.Controllers;

/// <summary>
/// Dedicated controller demonstrating ASP.NET Core Remote Validation.
/// Validates dynamic form values asynchronously from the browser via AJAX.
/// </summary>
public class ValidationController : Controller
{
    private readonly IWardService _wardService;

    public ValidationController(IWardService wardService)
    {
        _wardService = wardService;
    }

    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> ValidateWard(int wardId, CancellationToken ct)
    {
        if (wardId <= 0)
        {
            return Json("Please select a valid municipal ward.");
        }

        var exists = await _wardService.WardExistsAsync(wardId, ct);
        if (!exists)
        {
            return Json($"Municipal Ward #{wardId} was not found in city records.");
        }

        return Json(true);
    }
}
