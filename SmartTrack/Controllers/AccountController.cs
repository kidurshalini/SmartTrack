using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartTrack.Models;
using SmartTrack.ViewModel;
using SmartTrack.ViewModels;

namespace SmartTrack.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

       
        #region Register

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(HouseholdRegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var household = new HouseHoldDetails
            {
                HouseHoldId = Guid.NewGuid(),
                HouseHoldName = model.HouseHoldName,
                TotalMembers = model.TotalMembers,
                CreatedBy = model.OwnerUserName,
                CreatedOn = DateTime.Now
            };

            _context.HouseHoldDetails.Add(household);

            var owner = new ApplicationUser
            {
                UserName = model.OwnerUserName,
                Email = model.OwnerEmailId,
                PhoneNumber = model.OwnerPhoneNumber
            };

            var ownerResult = await _userManager.CreateAsync(owner, model.OwnerPassword);

            if (!ownerResult.Succeeded)
            {
                foreach (var error in ownerResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);
            }

            var ownerRoleResult = await _userManager.AddToRoleAsync(owner, "HouseholdOwner");

            if (!ownerRoleResult.Succeeded)
            {
                foreach (var error in ownerRoleResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);
            }

            _context.UserHouseHoldDetails.Add(new UserHouseHoldDetails
            {
                UserHouseHoldId = Guid.NewGuid(),
                UserId = owner.Id,
                HouseHoldId = household.HouseHoldId,
                CreatedBy = owner.Id,
                CreatedOn = DateTime.Now
            });

            if (model.Members != null && model.Members.Any())
            {
                foreach (var item in model.Members)
                {
                    if (string.IsNullOrWhiteSpace(item.UserName))
                    {
                        continue;
                    }

                    var user = new ApplicationUser
                    {
                        UserName = item.UserName,
                        Email = item.EmailId,
                        PhoneNumber = item.PhoneNumber
                    };

                    var result = await _userManager.CreateAsync(user, item.Password);

                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("",
                                $"Member {item.UserName}: {error.Description}");
                        }

                        return View(model);
                    }

                    var roleResult = await _userManager.AddToRoleAsync(
                        user,
                        string.IsNullOrWhiteSpace(item.Role)
                            ? "FamilyMembers"
                            : item.Role);

                    if (!roleResult.Succeeded)
                    {
                        foreach (var error in roleResult.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }

                        return View(model);
                    }

                    _context.UserHouseHoldDetails.Add(new UserHouseHoldDetails
                    {
                        UserHouseHoldId = Guid.NewGuid(),
                        UserId = user.Id,
                        HouseHoldId = household.HouseHoldId,
                        CreatedBy = owner.Id,
                        CreatedOn = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Household registered successfully!";

            return RedirectToAction(nameof(Success));
        }

        public IActionResult Success()
        {
            return View();
        }

        #endregion

        #region Login

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var user = await _userManager.FindByEmailAsync(model.Email);


            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }


            var result = await _signInManager.PasswordSignInAsync(
                user.UserName,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);



            if (result.Succeeded)
            {

                // Save user information in Session

                HttpContext.Session.SetString(
                    "UserId",
                    user.Id
                );


                HttpContext.Session.SetString(
                    "UserName",
                    user.UserName
                );


                HttpContext.Session.SetString(
                    "UserEmail",
                    user.Email
                );


                // Get user role

                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Any())
                {
                    HttpContext.Session.SetString(
                        "UserRole",
                        roles.First()
                    );
                }


                TempData["SuccessMessage"] =
                    "Login successfully!";


                return RedirectToAction("Index", "Home");
            }


            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    "",
                    "Your account has been locked."
                );

                return View(model);
            }


            ModelState.AddModelError(
                "",
                "Invalid email or password."
            );

            return View(model);
        }

        #endregion

        #region Logout

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }

        #endregion
    }
}