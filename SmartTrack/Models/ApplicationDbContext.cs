using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

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


            // User Household Relationship
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



            // Receipt Primary Key
            builder.Entity<ReceiptModel>()
                .HasKey(x => x.ReceiptId);



            // Receipt Item Primary Key
            builder.Entity<ReceiptItemModel>()
                .HasKey(x => x.ReceiptItemId);



            // Receipt -> ReceiptItems
            builder.Entity<ReceiptItemModel>()
                .HasOne(x => x.Receipt)
                .WithMany(x => x.ReceiptItems)
                .HasForeignKey(x => x.ReceiptId)
                .OnDelete(DeleteBehavior.Cascade);



            // User -> Receipts
            builder.Entity<ReceiptModel>()
                .HasOne(x => x.User)
                .WithMany(x => x.Receipts)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);



            // Decimal Precision
            builder.Entity<ReceiptItemModel>()
                .Property(x => x.TotalPrice)
                .HasPrecision(10, 2);


            builder.Entity<ReceiptModel>()
                .Property(x => x.TotalAmount)
                .HasPrecision(10, 2);

            builder.Entity<ReceiptItemModel>()
              .Property(x => x.UnitPrice)
              .HasPrecision(10, 2);

            builder.Entity<SmartTrackNotification>()
     .HasOne(x => x.HouseHold)
     .WithMany()
     .HasForeignKey(x => x.HouseHoldId)
     .OnDelete(DeleteBehavior.Cascade);


            // SmartTrack Notification -> User
            builder.Entity<SmartTrackNotification>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ShoppingListItem>()
    .HasOne(x => x.ShoppingList)
    .WithMany(x => x.Items)
    .HasForeignKey(x => x.ShoppingListId)
    .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ShoppingListItem>()
    .Property(x => x.Quantity)
    .HasPrecision(10, 2);
        }


       
        public DbSet<HouseHoldDetails> HouseHoldDetails { get; set; }

        public DbSet<UserHouseHoldDetails> UserHouseHoldDetails { get; set; }


        public DbSet<PasswordResetOtp> PasswordResetOtps { get; set; }


        public DbSet<ReceiptModel> Receipts { get; set; }


        public DbSet<ReceiptItemModel> ReceiptItems { get; set; }

        public DbSet<SmartTrackNotification> SmartTrackNotifications { get; set; }
        public DbSet<ShoppingList> ShoppingLists { get; set; }

        public DbSet<ShoppingListItem> ShoppingListItems { get; set; }

    }
}