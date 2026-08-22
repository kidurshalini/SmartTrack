using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartTrack.Models;
using SmartTrack.Services;
using SmartTrack.ViewModel;

namespace SmartTrack.Controllers
{
    [Authorize]
    public class SmartTrackDashboardController
        : Controller
    {
        private readonly UserManager<ApplicationUser>
            _userManager;

        private readonly SmartTrackDashboardService
            _dashboardService;


        public SmartTrackDashboardController(
            UserManager<ApplicationUser> userManager,

            SmartTrackDashboardService
                dashboardService)
        {
            _userManager =
                userManager;

            _dashboardService =
                dashboardService;
        }


        // =====================================================
        // DASHBOARD
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user =
                await _userManager
                    .GetUserAsync(
                        User);

            if (user == null)
            {
                return Challenge();
            }


            var model =
                await _dashboardService
                    .GetDashboardAsync(
                        user.Id);


            return View(model);
        }
    }
}