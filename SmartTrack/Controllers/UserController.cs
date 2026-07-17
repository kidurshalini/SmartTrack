using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTrack.Models;
using SmartTrack.ViewModel;
using System.Security.Claims;
using static SmartTrack.ViewModel.ProfileViewModel;

namespace SmartTrack.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<UserController> _logger;
     


        public UserController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
             RoleManager<IdentityRole> roleManager,
            ILogger<UserController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // GET: /User/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? HttpContext.Session.GetString("UserId");

                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound();

                var roles = await _userManager.GetRolesAsync(user);
                var userRole = roles.FirstOrDefault() ?? "User";

                var userHousehold = await _context.UserHouseHoldDetails
                    .Include(u => u.HouseHold)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (userHousehold == null)
                {
                    var noHouseholdModel = new ProfileViewModel
                    {
                        UserId = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        Role = userRole,
                        HouseHoldName = "Not associated with any household",
                        Members = new List<MemberViewModel>()
                    };
                    return View(noHouseholdModel);
                }

                var members = await _context.UserHouseHoldDetails
                    .Include(u => u.User)
                    .Where(u => u.HouseHoldId == userHousehold.HouseHoldId)
                    .ToListAsync();

                var memberViewModels = new List<MemberViewModel>();
                foreach (var m in members)
                {
                    var memberRoles = await _userManager.GetRolesAsync(m.User);
                    memberViewModels.Add(new MemberViewModel
                    {
                        UserId = m.User.Id,
                        UserName = m.User.UserName,
                        Email = m.User.Email,
                        PhoneNumber = m.User.PhoneNumber,
                        Role = memberRoles.FirstOrDefault() ?? "Member"
                    });
                }

                var model = new ProfileViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = userRole,
                    HouseHoldId = userHousehold.HouseHoldId,
                    HouseHoldName = userHousehold.HouseHold?.HouseHoldName ?? "N/A",
                    TotalMembers = userHousehold.HouseHold?.TotalMembers ?? 0,
                    Members = memberViewModels
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile for user.");
                TempData["ErrorMessage"] = "Unable to load profile. Please try again later.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: Edit Household Name
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditHousehold(string householdName)
        {
            var userId = HttpContext.Session.GetString("UserId")
                         ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var userHousehold = await _context.UserHouseHoldDetails
                .Include(uh => uh.HouseHold)
                .FirstOrDefaultAsync(uh => uh.UserId == userId);

            if (userHousehold == null)
            {
                TempData["ErrorMessage"] = "Household not found.";
                return RedirectToAction(nameof(Profile));
            }

            var household = userHousehold.HouseHold;
            household.HouseHoldName = householdName.Trim();
            household.ModifiedOn = DateTime.Now;
            household.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Household name updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        // POST: Edit Current User's Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(string username, string email, string phone)
        {
            var userId = HttpContext.Session.GetString("UserId")
                         ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            // Check uniqueness
            var existingUser = await _userManager.FindByNameAsync(username);
            if (existingUser != null && existingUser.Id != userId)
            {
                TempData["ErrorMessage"] = "Username is already taken.";
                return RedirectToAction(nameof(Profile));
            }

            existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null && existingUser.Id != userId)
            {
                TempData["ErrorMessage"] = "Email is already registered.";
                return RedirectToAction(nameof(Profile));
            }

            user.UserName = username;
            user.Email = email;
            user.PhoneNumber = phone;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Failed to update profile: " + string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Profile));
            }

            // Update session
            HttpContext.Session.SetString("UserName", user.UserName);
            HttpContext.Session.SetString("UserEmail", user.Email);

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(
       [FromForm][Bind(Prefix = "AddMemberInput")] ProfileViewModel.AddMemberInputModel model)
        {
            if (model == null)
            {
                TempData["ErrorMessage"] = "Invalid member data.";
                return RedirectToAction(nameof(Profile));
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the input errors.";
                return RedirectToAction(nameof(Profile));
            }

            var currentUserId = HttpContext.Session.GetString("UserId")
                                ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
                return RedirectToAction("Login", "Account");

            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            var currentRoles = await _userManager.GetRolesAsync(currentUser);
            if (!currentRoles.Contains("HouseholdOwner"))
            {
                TempData["ErrorMessage"] = "Only the household owner can add members.";
                return RedirectToAction(nameof(Profile));
            }

            var userHousehold = await _context.UserHouseHoldDetails
                .FirstOrDefaultAsync(uh => uh.UserId == currentUserId);

            if (userHousehold == null)
            {
                TempData["ErrorMessage"] = "You are not associated with a household.";
                return RedirectToAction(nameof(Profile));
            }

            var householdId = userHousehold.HouseHoldId;

            // Check existing
            var existingUser = await _userManager.FindByNameAsync(model.UserName);
            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "Username already exists.";
                return RedirectToAction(nameof(Profile));
            }

            existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "Email already registered.";
                return RedirectToAction(nameof(Profile));
            }

            // Create user
            var newUser = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber
            };

            var createResult = await _userManager.CreateAsync(newUser, model.Password);
            if (!createResult.Succeeded)
            {
                TempData["ErrorMessage"] = "Failed to create user: " +
                    string.Join(", ", createResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Profile));
            }

            // Ensure role exists
            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(model.Role));
            }

            // Assign role
            var roleResult = await _userManager.AddToRoleAsync(newUser, model.Role);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(newUser);
                TempData["ErrorMessage"] = "Failed to assign role: " +
                    string.Join(", ", roleResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Profile));
            }

            // Link to household
            _context.UserHouseHoldDetails.Add(new UserHouseHoldDetails
            {
                UserHouseHoldId = Guid.NewGuid(),
                UserId = newUser.Id,
                HouseHoldId = householdId,
                CreatedBy = currentUserId,
                CreatedOn = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Member {model.UserName} added successfully.";
            return RedirectToAction(nameof(Profile));
        }

        // POST: Edit Member
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMember(string userId, string username, string email, string phone, string role)
        {
            var currentUserId = HttpContext.Session.GetString("UserId")
                                ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
                return RedirectToAction("Login", "Account");

            // Only owner can edit members
            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            var currentRoles = await _userManager.GetRolesAsync(currentUser);
            if (!currentRoles.Contains("HouseholdOwner"))
            {
                TempData["ErrorMessage"] = "Only the household owner can edit members.";
                return RedirectToAction(nameof(Profile));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Profile));
            }

            // Check uniqueness
            var existingUser = await _userManager.FindByNameAsync(username);
            if (existingUser != null && existingUser.Id != userId)
            {
                TempData["ErrorMessage"] = "Username is already taken.";
                return RedirectToAction(nameof(Profile));
            }

            existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null && existingUser.Id != userId)
            {
                TempData["ErrorMessage"] = "Email is already registered.";
                return RedirectToAction(nameof(Profile));
            }

            user.UserName = username;
            user.Email = email;
            user.PhoneNumber = phone;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                TempData["ErrorMessage"] = "Failed to update user: " + string.Join(", ", updateResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Profile));
            }

            // Update role
            var currentRolesList = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRolesList);
            await _userManager.AddToRoleAsync(user, role);

            TempData["SuccessMessage"] = $"Member {username} updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        // POST: Delete Member (remove from household)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMember(string userId)
        {
            var currentUserId = HttpContext.Session.GetString("UserId")
                                ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
                return RedirectToAction("Login", "Account");

            // Only owner can delete members
            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            var currentRoles = await _userManager.GetRolesAsync(currentUser);
            if (!currentRoles.Contains("HouseholdOwner"))
            {
                TempData["ErrorMessage"] = "Only the household owner can remove members.";
                return RedirectToAction(nameof(Profile));
            }

            var link = await _context.UserHouseHoldDetails
                .FirstOrDefaultAsync(uh => uh.UserId == userId);

            if (link == null)
            {
                TempData["ErrorMessage"] = "User is not a member of this household.";
                return RedirectToAction(nameof(Profile));
            }

            _context.UserHouseHoldDetails.Remove(link);
            await _context.SaveChangesAsync();

         
            TempData["SuccessMessage"] = "Member removed from household.";
            return RedirectToAction(nameof(Profile));
        }
    }
}