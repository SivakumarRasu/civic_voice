using System.Security.Claims;
using CivicVoice.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicVoice.Web.Controllers;

[Authorize]
public class FeedbackController : Controller
{
    private readonly IFeedbackService _feedbackService;

    public FeedbackController(IFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int grievanceId, int rating, string? comments, CancellationToken ct)
    {
        var citizenId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
        if (string.IsNullOrEmpty(citizenId))
        {
            return Unauthorized();
        }

        var result = await _feedbackService.SubmitFeedbackAsync(grievanceId, citizenId, rating, comments, ct);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = "Thank you! Your feedback has been submitted successfully.";
        }

        return RedirectToAction("Details", "Grievances", new { id = grievanceId });
    }
}
