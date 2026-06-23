using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartTrack.Models;
using System.Diagnostics;

namespace SmartTrack.Common
{
    public class SeedData
    {
        public static async Task SeedRole(IServiceProvider serviceProvider)
        {
            var scope = serviceProvider.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();


            var roles = new List<IdentityRole>
            {
                new IdentityRole { Name = CustomRole.SystemAdministrator, NormalizedName = CustomRole.SystemAdministrator },
                new IdentityRole { Name = CustomRole.FamilyMembers, NormalizedName = CustomRole.FamilyMembers },
                new IdentityRole { Name = CustomRole.HourseMaid, NormalizedName = CustomRole.HourseMaid },
                new IdentityRole { Name = CustomRole.HouseholdOwner, NormalizedName = CustomRole.HouseholdOwner }

            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role.Name))
                {
                    await roleManager.CreateAsync(role);
                }
            }
        }
    }

    public static class CustomRole
    {
        public const string SystemAdministrator = "SystemAdministrator";
        public const string FamilyMembers = "FamilyMembers"; 
        public const string HourseMaid = "HouseMaid";
        public const string HouseholdOwner = "HouseholdOwner";  
    }

}
