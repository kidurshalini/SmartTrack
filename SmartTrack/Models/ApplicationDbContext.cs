using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SmartTrack.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UserHouseHoldDetails>()
                .HasOne(x => x.User)
                .WithMany(x => x.UserHouseHolds)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserHouseHoldDetails>()
                .HasOne(x => x.HouseHold)
                .WithMany(x => x.UserHouseHolds)
                .HasForeignKey(x => x.HouseHoldId)
                .OnDelete(DeleteBehavior.Cascade);

        }

        public DbSet<HouseHoldDetails> HouseHoldDetails { get; set; }
        public DbSet<UserHouseHoldDetails> UserHouseHoldDetails { get; set; }

    }
}