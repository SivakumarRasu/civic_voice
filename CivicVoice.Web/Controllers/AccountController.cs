using CivicVoice.Application.Interfaces;
using CivicVoice.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CivicVoice.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IDepartmentService _departmentService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IDepartmentService departmentService,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _roleManager = roleManager;
        _departmentService = departmentService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var user = await _userManager.FindByEmailAsync(vm.EmailOrUsername)
                   ?? await _userManager.FindByNameAsync(vm.EmailOrUsername);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            return View(vm);
        }

        var result = await _signInManager.PasswordSignInAsync(user, vm.Password, vm.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            _logger.LogInformation("User {User} logged in successfully.", user.Email);

            if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
            {
                return Redirect(vm.ReturnUrl);
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Commissioner"))
            {
                return RedirectToAction("Dashboard", "Commissioner");
            }
            if (roles.Contains("DepartmentOfficer"))
            {
                return RedirectToAction("Dashboard", "Officer");
            }

            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, "Invalid password.");
        return View(vm);
    }

    /// <summary>
    /// Quick 1-click login action specifically designed for academic demonstration.
    /// Allows the evaluator to switch seamlessly between Citizen, Officer, and Commissioner.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> QuickLogin(string role, string? returnUrl = null)
    {
        string email = role.ToLower() switch
        {
            "commissioner" => "commissioner@civicvoice.local",
            "officer" => "roads_officer@civicvoice.local",
            "water" => "water_officer@civicvoice.local",
            _ => "citizen@civicvoice.local"
        };

        var user = await _userManager.FindByEmailAsync(email);
        if (user != null)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
            TempData["SuccessMessage"] = $"Logged in as demo user: {email} ({role})";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (role.Equals("commissioner", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Dashboard", "Commissioner");
            if (role.Equals("officer", StringComparison.OrdinalIgnoreCase) || role.Equals("water", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Dashboard", "Officer");

            return RedirectToAction("Index", "Home");
        }

        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public async Task<IActionResult> Register(CancellationToken ct)
    {
        var vm = new RegisterViewModel();
        await PopulateDepartmentsAsync(vm, ct);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDepartmentsAsync(vm, ct);
            return View(vm);
        }

        var user = new IdentityUser { UserName = vm.Email, Email = vm.Email, EmailConfirmed = true };
        var result = await _userManager.CreateAsync(user, vm.Password);

        if (result.Succeeded)
        {
            // Ensure role exists
            if (!await _roleManager.RoleExistsAsync(vm.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(vm.Role));
            }

            await _userManager.AddToRoleAsync(user, vm.Role);
            await _signInManager.SignInAsync(user, isPersistent: false);

            _logger.LogInformation("New user {Email} registered with role {Role}.", vm.Email, vm.Role);
            TempData["SuccessMessage"] = $"Welcome {vm.FullName}! Your account has been registered as {vm.Role}.";

            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        await PopulateDepartmentsAsync(vm, ct);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["SuccessMessage"] = "You have been logged out.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task PopulateDepartmentsAsync(RegisterViewModel vm, CancellationToken ct)
    {
        var depts = await _departmentService.GetAllDepartmentsAsync(ct);
        vm.Departments = depts.Select(d => new SelectListItem(d.Name, d.DepartmentId.ToString())).ToList();
    }
}
