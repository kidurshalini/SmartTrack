using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTrack.Models;
using SmartTrack.ViewModel;
using SmartTrack.ViewModels;
using System.Net;
using System.Net.Mail;
namespace SmartTrack.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ILogger<AccountController> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Register a new household along with the owner and members
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(HouseholdRegisterViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // store house hold details
                var household = new HouseHoldDetails
                {
                    HouseHoldId = Guid.NewGuid(),
                    HouseHoldName = model.HouseHoldName,
                    TotalMembers = model.TotalMembers,
                    CreatedBy = model.OwnerUserName,
                    CreatedOn = DateTime.Now
                };

                _context.HouseHoldDetails.Add(household);

                // store Owner details
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

                // Checking members are added or not and store those things
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
                                ModelState.AddModelError("", $"Member {item.UserName}: {error.Description}");
                            }
                            return View(model);
                        }

                        var roleResult = await _userManager.AddToRoleAsync(
                            user,
                            string.IsNullOrWhiteSpace(item.Role) ? "FamilyMembers" : item.Role);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during household registration.");
                ModelState.AddModelError("", "An unexpected error occurred. Please try again later.");
                return View(model);
            }
        }

        public IActionResult Success()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        //login user and set session variables
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            try
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
                    HttpContext.Session.SetString("UserId", user.Id);
                    HttpContext.Session.SetString("UserName", user.UserName);
                    HttpContext.Session.SetString("UserEmail", user.Email);


                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Any())
                    {
                        HttpContext.Session.SetString("UserRole", roles.First());
                    }

                    TempData["SuccessMessage"] = "Login successfully!";
                    return RedirectToAction("Index", "Home");
                }

                if (result.IsLockedOut)
                {
                    ModelState.AddModelError("", "Your account has been locked.");
                    return View(model);
                }

                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email {Email}", model.Email);
                ModelState.AddModelError("", "An unexpected error occurred. Please try again later.");
                return View(model);
            }
        }

        //logout user and clear session variables
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _signInManager.SignOutAsync();
                HttpContext.Session.Clear();
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout.");
                return RedirectToAction("Login");
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // Handle forgot password request and send OTP to user's email
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "Email not found.");
                    return View(model);
                }

                Random random = new Random();
                string otp = random.Next(100000, 999999).ToString();

                var otpRecord = new PasswordResetOtp
                {
                    UserId = user.Id,
                    OtpCode = otp,
                    ExpiryTime = DateTime.Now.AddMinutes(5),
                    IsUsed = false
                };

                _context.PasswordResetOtps.Add(otpRecord);
                await _context.SaveChangesAsync();

                await SendOtpEmail(user.Email, otp);

                return RedirectToAction("VerifyOtp", new { email = user.Email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password for email {Email}", model?.Email);
                ModelState.AddModelError("", "Unable to process your request. Please try again later.");
                return View(model);
            }
        }

        private async Task SendOtpEmail(string email, string otp)
        {
            try
            {
                string smtpUser = _configuration["EmailSettings:SmtpUser"];
                string smtpPassword = _configuration["EmailSettings:SmtpPassword"];

                MailMessage message = new MailMessage
                {
                    From = new MailAddress(smtpUser, "SmartTrack"),
                    Subject = "SmartTrack Password Reset OTP",
                    IsBodyHtml = true,
                    Body = $@"
                        <!DOCTYPE html>
                        <html>
                        <head>
                            <meta charset='UTF-8'>

                            <link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css' rel='stylesheet'>
                        </head>
                        <body class='bg-light'>

                        <div class='container mt-5'>
                            <div class='card shadow mx-auto' style='max-width:600px;'>

                                <div class='card-header bg-success text-white text-center py-4'>
                                    <h2 class='mb-0'>🔐 SmartTrack</h2>
                                    <p class='mb-0'>Password Reset Verification</p>
                                </div>

                                <div class='card-body p-5'>

                                    <h4>Hello,</h4>

                                    <p>
                                        We received a request to reset your password.
                                        Use the OTP below to continue.
                                    </p>

                                    <div class='alert alert-success text-center'>
                                        <small>Your One-Time Password</small>
                                        <h1 class='display-4 fw-bold'>{otp}</h1>
                                    </div>

                                    <p>
                                        <strong>This OTP expires in 5 minutes.</strong>
                                    </p>

                                    <p class='text-muted'>
                                        If you didn't request this password reset,
                                        you can safely ignore this email.
                                    </p>

                                </div>

                                <div class='card-footer text-center text-muted'>
                                    <strong>Thank you,</strong><br>
                                    SmartTrack Team
                                </div>

                            </div>
                        </div>

                        </body>
                        </html>"
                };
                message.To.Add(email);

                using (SmtpClient smtp = new SmtpClient())
                {
                    smtp.Host = "smtp.gmail.com";
                    smtp.Port = 587;
                    smtp.EnableSsl = true;
                    smtp.Timeout = 10000;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(smtpUser, smtpPassword);

                    await smtp.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}", email);
                throw; // rethrow to be caught in the calling action
            }
        }

        [HttpGet]
        public IActionResult VerifyOtp(string email)
        {
            return View(new VerifyOtpViewModel { Email = email });
        }

        // Handle OTP verification with database and redirect to password reset page if valid
        [HttpPost]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "User not found.");
                    return View(model);
                }

                var otpRecord = await _context.PasswordResetOtps
                    .Where(x => x.UserId == user.Id &&
                                x.OtpCode == model.Otp &&
                                !x.IsUsed)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();

                if (otpRecord == null)
                {
                    ModelState.AddModelError("", "Invalid OTP.");
                    return View(model);
                }

                if (otpRecord.ExpiryTime < DateTime.Now)
                {
                    ModelState.AddModelError("", "OTP Expired.");
                    return View(model);
                }

                otpRecord.IsUsed = true;
                await _context.SaveChangesAsync();

                return RedirectToAction("ResetPassword", new { email = model.Email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying OTP for email {Email}", model?.Email);
                ModelState.AddModelError("", "An error occurred while verifying OTP.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            return View(new ResetPasswordViewModel { Email = email });
        }

        // Handle password reset after OTP verification
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "User not found.");
                    return View(model);
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(model);
                }

                TempData["Success"] = "Password reset successfully.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for email {Email}", model?.Email);
                ModelState.AddModelError("", "An error occurred while resetting password.");
                return View(model);
            }
        }
    }
}