using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        public AccountController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Register Household and Owner
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(HouseholdRegisterViewModel model)
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

            // store Owner role details
            var ownerRoleResult = await _userManager.AddToRoleAsync(owner, "HouseholdOwner");

            if (!ownerRoleResult.Succeeded)
            {
                foreach (var error in ownerRoleResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);
            }

            // store Owner household details
            _context.UserHouseHoldDetails.Add(new UserHouseHoldDetails
            {
                UserHouseHoldId = Guid.NewGuid(),
                UserId = owner.Id,
                HouseHoldId = household.HouseHoldId,
                CreatedBy = owner.Id,
                CreatedOn = DateTime.Now
            });

            //Checking members are added or not and store those things
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
   
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        //Login to the system
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

            //checking assword and username 
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);



            if (result.Succeeded)
            {

                // Save user information in Session

                HttpContext.Session.SetString( "UserId",  user.Id );

                HttpContext.Session.SetString("UserName", user.UserName);

                HttpContext.Session.SetString(  "UserEmail",user.Email );

                // Get user role

                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Any())
                {
                    HttpContext.Session.SetString("UserRole",roles.First() );
                }


                TempData["SuccessMessage"] = "Login successfully!";


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
   
        // logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        } 

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
              
            //Finding user emailid is there or not
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Email not found.");
                return View(model);
            }

            //Create random otp
            Random random = new Random();

            string otp = random.Next(100000, 999999).ToString();

            //save otp in database
            var otpRecord = new PasswordResetOtp
            {
                UserId = user.Id,
                OtpCode = otp,
                ExpiryTime = DateTime.Now.AddMinutes(5),
                IsUsed = false
            };

            _context.PasswordResetOtps.Add(otpRecord);
            await _context.SaveChangesAsync();

            //Send Email Here
            await SendOtpEmail(user.Email, otp);

            return RedirectToAction("VerifyOtp", new { email = user.Email });
        }

        //send email
        private async Task SendOtpEmail(string email, string otp)
        {
            try
            {
                string smtpUser = _configuration["EmailSettings:SmtpUser"];
                string smtpPassword = _configuration["EmailSettings:SmtpPassword"];

                MailMessage message = new MailMessage();

                message.From = new MailAddress( smtpUser, "SmartTrack" );

                message.To.Add(email);

                message.Subject = "SmartTrack Password Reset OTP";

                message.Body = $@"
                            SmartTrack Password Reset

                            Your OTP is:

                            {otp}

                            This OTP expires in 5 minutes.

                            Thank you,
                            SmartTrack Team";

                using (SmtpClient smtp = new SmtpClient())
                {
                    smtp.Host = "smtp.gmail.com";
                    smtp.Port = 587;
                    smtp.EnableSsl = true;
                    smtp.Timeout = 10000; 
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(
                        smtpUser,
                        smtpPassword
                    );

                    await smtp.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        [HttpGet]
        public IActionResult VerifyOtp(string email)
        {
            return View(new VerifyOtpViewModel
            {
                Email = email
            });
        }

        //verify the otp
        [HttpPost]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            //user find by email id
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return View(model);
            }
            //Check otp is correct from the table
            var otpRecord = await _context.PasswordResetOtps
                .Where(x =>
                    x.UserId == user.Id &&
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

            return RedirectToAction("ResetPassword",new { 
                email = model.Email 
            });
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            return View(new ResetPasswordViewModel
            {
                Email = email
            });
        }

        //reset password

        [HttpPost]
        public async Task<IActionResult> ResetPassword( ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return View(model);
            }
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = await _userManager.ResetPasswordAsync(
                    user,
                    token,
                    model.NewPassword);

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
    }
}
