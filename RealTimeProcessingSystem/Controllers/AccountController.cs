using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RealTimeProcessingSystem.Models;
using RealTimeProcessingSystem.Services;

namespace RealTimeProcessingSystem.Controllers;

public sealed class AccountController : Controller
{
    private readonly FarmUserService _users;
    private readonly ILogger<AccountController> _logger;

    public AccountController(FarmUserService users, ILogger<AccountController> logger)
    {
        _users = users;
        _logger = logger;
    }

    [AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await _users.RegisterFarmerAsync(model.Username, model.Password, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Username), "That username is already registered.");
            return View(model);
        }

        await SignInAsync(new FarmUserSummary(model.Username.Trim(), "Farmer"));
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { Username = string.Empty });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var user = await _users.AuthenticateAsync(model.Username, model.Password, cancellationToken);
        if (user is null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ModelState.AddModelError(string.Empty, "Username or password is incorrect.");
            return View(model);
        }

        await SignInAsync(user);
        return Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl!)
            : RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Users(CancellationToken cancellationToken)
    {
        var users = await _users.GetUsersAsync(cancellationToken);
        return View(users);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(ChangeRoleViewModel model, CancellationToken cancellationToken)
    {
        if (model.SystemRole is not ("Farmer" or "Agronomist" or "Admin"))
        {
            return BadRequest("System role must be Farmer, Agronomist, or Admin.");
        }

        if (!await _users.ChangeRoleAsync(model.Username, model.SystemRole, cancellationToken))
        {
            return NotFound();
        }

        _logger.LogInformation("Administrator {AdminUser} changed the role for {TargetUser}.", User.Identity?.Name, model.Username);
        return RedirectToAction(nameof(Users));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private async Task SignInAsync(FarmUserSummary user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.SystemRole)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }
}
