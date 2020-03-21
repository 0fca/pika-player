using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Claudia.Models;
using Claudia.Models.AccountViewModels;
using Claudia.Models.ManageViewModels;
using Claudia.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Claudia.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IGenerator _urlGeneratorService;

        public ManageController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<IdentityRole> roleManager,
            IGenerator urlGeneratorService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _urlGeneratorService = urlGeneratorService;
        }
        
        
        [TempData] private static string StatusMessage { get; set; }
        [TempData] private static string ReturnMessage { get; set; }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var model = new IndexViewModel
            {
                Username = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsEmailConfirmed = user.EmailConfirmed,
                StatusMessage = StatusMessage
            };
            StatusMessage = null;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(IndexViewModel model)
        {
            StatusMessage = "The profile couldn't be updated.";
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var email = user.Email;
            if (model.Email != email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    throw new ApplicationException($"Unexpected error occurred setting email for user with ID '{user.Id}'.");
                }
            }

            var phoneNumber = user.PhoneNumber;
            if (model.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, model.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    throw new ApplicationException($"Unexpected error occurred setting phone number for user with ID '{user.Id}'.");
                }
            }

            StatusMessage = "Your profile has been updated";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminPanel()
        {
            var usersWithRoles = new Dictionary<User, IList<string>>();

            if (usersWithRoles.Count == 0)
            {
                await _userManager.Users.ToAsyncEnumerable().ForEachAsync(user =>
                {
                    var roles = _userManager.GetRolesAsync(user);
                    usersWithRoles.Add(user, roles.Result);
                });
            }

            var adminPanelViewModel = new AdminPanelViewModel {UsersWithRoles = usersWithRoles};

            ViewData["returnMessage"] = TempData["returnMessage"];
            return View("/Views/Manage/Admin/AdminUserPanel.cshtml", adminPanelViewModel);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> GeneratePassword(string id)
        {
            var userModel = await _userManager.FindByIdAsync(id);
            if ((await _userManager.GetLoginsAsync(userModel)).Count == 0) {
                var token = await _userManager.GeneratePasswordResetTokenAsync(userModel);
                var guid = Guid.NewGuid().ToString();
                _urlGeneratorService.SetDerivationPrf(KeyDerivationPrf.HMACSHA256);
                var hash = _urlGeneratorService.GenerateId(guid);
                var result = await _userManager.ResetPasswordAsync(userModel, token, hash);
                TempData["newPassword"] = hash;
            }
            ReturnMessage = "This user is logged in via 3rd party provider, cannot reset password.";
            return RedirectToAction(nameof(AdminPanel));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditUser(string id)
        {
            var userModel = await _userManager.FindByIdAsync(id);
            var editUserModel = new EditUserModel
            {
                Id = userModel.Id,
                Phone = userModel.PhoneNumber,
                Email = userModel.Email,
                UserName = userModel.UserName,
                Roles = await _userManager.GetRolesAsync(userModel)
            };
            return View("/Views/Manage/Admin/Edit.cshtml", editUserModel);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditUserConfirmation(EditUserModel editModel)
        {
            var userModel = await _userManager.FindByIdAsync(editModel.Id);
            userModel.Email = editModel.Email;
            userModel.UserName = editModel.UserName;
            userModel.PhoneNumber = editModel.Phone;
            if (editModel.Roles != null) {
                await _userManager.AddToRolesAsync(userModel, editModel.Roles);
            }
            var result = await _userManager.UpdateAsync(userModel);
            TempData["returnMessage"] = result.Succeeded ? "Successfully edited user's information." : "Could not edit user's information.";
            return RedirectToAction(nameof(AdminPanel));
        }

        [HttpGet]
        [AutoValidateAntiforgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveFromRole(string id, string roleName) {
            var user = await _userManager.FindByIdAsync(id);
            if ((await _userManager.GetRolesAsync(user)).Count > 1)
            {
                var result = await _userManager.RemoveFromRoleAsync(user, roleName);
                if (!result.Succeeded)
                {
                    StatusMessage = "User of id " + id + " couldn't be deleted from role: " + roleName;
                    //_loggerService.LogToFileAsync(LogLevel.Error, HttpContext.Connection.RemoteIpAddress.ToString(), StatusMessage + "\n" + result.Errors);
                }
            }
            else
            {
                StatusMessage = "Couldn't delete user of id "+id+" from role "+roleName+", user has to be in one role at least.";
            }
            return RedirectToAction(nameof(EditUser), new { @Id = id});
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var email = await _userManager.FindByIdAsync(id);
            return View("/Views/Manage/Admin/Delete.cshtml", email);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUserConfirmation(string id)
        {
            var result = await _userManager.DeleteAsync(await _userManager.FindByIdAsync(id));
            TempData["returnMessage"] = result.Succeeded ? "Successfully deleted user of id " + id : "Could not delete user of id " + id;
            return RedirectToAction(nameof(AdminPanel));
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (!hasPassword)
            {
                return RedirectToAction(nameof(SetPassword));
            }

            var model = new ChangePasswordViewModel { StatusMessage = StatusMessage };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                AddErrors(changePasswordResult);
                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            //_loggerService.LogToFileAsync(LogLevel.Information, HttpContext.Request.Host.Value, "User changed their password successfully.");
            StatusMessage = "Your password has been changed.";

            return RedirectToAction(nameof(ChangePassword));
        }
        
        [HttpGet]
        public async Task<IActionResult> SetPassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var hasPassword = await _userManager.HasPasswordAsync(user);

            if (hasPassword)
            {
                return RedirectToAction(nameof(ChangePassword));
            }

            var model = new SetPasswordViewModel { StatusMessage = StatusMessage };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var addPasswordResult = await _userManager.AddPasswordAsync(user, model.NewPassword);
            if (!addPasswordResult.Succeeded)
            {
                AddErrors(addPasswordResult);
                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            StatusMessage = "Your password has been set.";

            return RedirectToAction(nameof(SetPassword));
        }

        #region HelperMethods

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        #endregion
    }
}