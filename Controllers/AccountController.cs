using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Voicer.Models;
using Voicer.Data;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Voicer.Service;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly EmailService _emailService;
    private readonly IVoiceService _voiceService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, EmailService emailService, IVoiceService voiceService, ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _voiceService = voiceService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Manage()
    {
        var user = await _userManager.GetUserAsync(User);
        var model = new ManageAccountViewModel
        {
            Username = user.UserName,
            PhoneNumber = user.PhoneNumber
        };
        return View(model);
    }

    [HttpPost("profile/updateUsername")]
    [HttpPost("profile/updateUsername")]
    public async Task<IActionResult> UpdateUsername(UserProfileViewModel model)
    {
        _logger.LogInformation("UpdateUsername called with username: {Username}", model.Username);

        if (ModelState.IsValid)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User ID from claims: {UserId}", userId);

            if (userId != null)
            {
                try
                {
                    bool success = await _voiceService.ChangeUsernameAsync(userId, model.Username);
                    if (success)
                    {
                        _logger.LogInformation("Username updated successfully.");
                        ViewBag.Message = "Username updated successfully!";
                    }
                    else
                    {
                        _logger.LogWarning("Failed to update username.");
                        ViewBag.Message = "Failed to update username.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while updating the username.");
                    ViewBag.Message = "Unexpected error when trying to set user name.";
                }
            }
            else
            {
                _logger.LogError("User ID is null.");
                ViewBag.Message = "User ID is not found.";
            }
        }
        else
        {
            _logger.LogWarning("Model state is invalid.");
        }
        return View("Profile", model);
    }


    [HttpPost]
    public async Task<IActionResult> Manage(ManageAccountViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                user.UserName = model.Username;
                user.PhoneNumber = model.PhoneNumber;
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    ViewData["Message"] = "Your profile has been updated";
                    return RedirectToAction("Manage");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code }, protocol: HttpContext.Request.Scheme);
            await _emailService.SendEmailAsync(model.Email, "Reset Password",
                $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ResetPassword(string code = null)
    {
        return code == null ? View("Error") : View(new ResetPasswordViewModel { Code = code });
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return RedirectToAction("ResetPasswordConfirmation");
        }
        var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
        if (result.Succeeded)
        {
            return RedirectToAction("ResetPasswordConfirmation");
        }
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View();
    }

    [HttpGet]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }
}
