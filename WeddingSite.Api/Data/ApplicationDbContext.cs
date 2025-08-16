using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace WeddingSite.Api.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {

        static ApplicationDbContext()
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        public DbSet<WeddingMessage> WeddingMessages { get; set; } = null!;

        public DbSet<WeddingParticipation> WeddingParticipations { get; set; } = null!;

        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }

        public DbSet<UserUploadedPhoto> UserUploadedPhotos { get; set; }

        public DbSet<WeddingGift> WeddingGifts { get; set; } = null!;

        public DbSet<UserActionLog> UserActionLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<WeddingMessage>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId);

            builder.Entity<WeddingParticipation>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId);

            builder.Entity<UserRefreshToken>(entity => {
                entity.HasOne(m => m.User)
               .WithMany()
               .HasForeignKey(m => m.UserId)
               .OnDelete(DeleteBehavior.NoAction);

                entity.Property(e => e.RefreshToken).HasColumnType("VARCHAR").HasMaxLength(250);
            });

            builder.Entity<UserUploadedPhoto>()
               .HasOne(m => m.User)
               .WithMany()
               .HasForeignKey(m => m.UserId)
               .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<WeddingGift>()
               .HasOne(m => m.User)
               .WithMany()
               .HasForeignKey(m => m.UserId)
               .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<UserActionLog>()
               .HasOne(m => m.User)
               .WithMany()
               .HasForeignKey(m => m.UserId)
               .OnDelete(DeleteBehavior.NoAction);

        }
    }
}
