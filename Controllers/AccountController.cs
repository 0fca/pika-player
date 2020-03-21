using System;
using System.Threading.Tasks;
using Claudia.Models;
using Claudia.Models.AccountViewModels;
using Claudia.Services;
using FMS2.Models.AccountViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Claudia.Controllers
{
    /**
     * <summary>This class is responsible for Account management of user in TSL Web SMS Service.</summary>
     * 
     */
    [Authorize]
    public class AccountController : Controller
    {
        //Objects injected by a DI container using default constructor.
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger _logger;
        private readonly IGenerator _generator;

        public AccountController(UserManager<User> userManager, 
                                 SignInManager<User> signInManager, 
                                 ILogger<AccountController> logger,
                                 IGenerator generator)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _generator = generator;
        }

        //This field is annotated to be used in TempData; it will contain ErrorMessage if needed.
        [TempData] public string ErrorMessage { get; set; }

        //HTTP GET verb; url: /account/login
        /**
         * <summary>This action returns log in page.</summary>
         * <param name="returnUrl">Return url string.</param>
         */
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        //HTTP POST verb; url: /account/login?model=[model]&returnUrl=url
        /**
         * <summary>This action is used when submitting form on Login page.</summary>
         * <param name="model">LoginViewModel object which contains form data.</param>
         * <param name="returnUrl"></param>
         */
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe,
                    lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return RedirectToLocal(returnUrl);
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToAction(nameof(Lockout));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }
        
        //HTTP GET verb; url: /account/lockout
        /**
         * <summary>Returns a view of lockin out the account.</summary>
         */
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        //HTTP GEt verb; url: /account/register
        /**
         * <summary>Returns a view for Register view.</summary>
         * <param name="returnUrl">Return url string.</param>
         * <remarks>Only for administrator purposes.</remarks>
         */
        [HttpGet]
        [Authorize(Policy = "RequireElevated")]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
        
        //HTTP POST verb; url: /account/register
        /**
         * <summary>Used for submission data from register view.</summary>
         * <remarks>Accessible only for admin purposes.</remarks>
         * 
         */

        [HttpPost]
        [Authorize(Policy = "RequireElevated")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);
            
            var user = new User { UserName = model.Email, Email = model.Email };
            var password = _generator.GeneratePassword();
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                await _userManager.AddToRoleAsync(user, "Student");
                _logger.LogInformation("User created a new account with password.");
                TempData["newPassword"] = password;
                return RedirectToAction("AdminPanel","Manage");
            }
            AddErrors(result);

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                // Don't reveal that the user does not exist or is not confirmed
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            /* code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.ResetPasswordCallbackLink(user.Id, code, Request.Scheme);
            await _emailSender.SendEmailAsync(model.Email, "Reset Password",
               $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");*/
            
            return RedirectToAction(nameof(ForgotPasswordConfirmation));

            // If we got this far, something failed, redisplay form
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            if (code == null)
            {
                throw new ApplicationException("A code must be supplied for password reset.");
            }
            var model = new ResetPasswordViewModel { Code = code };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            AddErrors(result);
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
        
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }


        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
        #endregion
    }
}