
using Codino.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Codino.Api.Data
{
    public class CodinoDbContext : DbContext
    {
        public CodinoDbContext(
            DbContextOptions<CodinoDbContext> options)
            : base(options)
        {
        }

        // جداول اصلی
        public DbSet<User> Users => Set<User>();
        public DbSet<Comment> Comments => Set<Comment>();
        // جدول درخواست‌های پروژه برنامه‌نویسی
        public DbSet<ProjectRequest> ProjectRequests =>
            Set<ProjectRequest>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureUser(modelBuilder);
            ConfigureComment(modelBuilder);
            ConfigureCourse(modelBuilder);
        }

        private static void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.FullName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.PhoneNumber)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(x => x.ProfileImageUrl)
                    .HasMaxLength(500)
                    .IsRequired(false);

                entity.Property(x => x.PasswordHash)
                    .IsRequired();

                entity.Property(x => x.IsPhoneVerified)
                    .IsRequired();

                entity.Property(x => x.CreatedAt)
                    .IsRequired();

                entity.Property(x => x.SecurityCode1Hash)
                    .IsRequired();

                entity.Property(x => x.SecurityCode2Hash)
                    .IsRequired();

                // این ویژگی در دیتابیس ذخیره نمی‌شود.
                entity.Ignore(x => x.IsActive);

                entity.Property(x => x.TotalPoints)
                    .IsRequired()
                    .HasDefaultValue(0);

                entity.Property(x => x.ShowInHallOfFame)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.HasIndex(x => x.PhoneNumber)
                    .IsUnique();

                entity.HasIndex(x => new
                {
                    x.ShowInHallOfFame,
                    x.TotalPoints
                });
            });
        }

        private static void ConfigureComment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.ToTable("Comments", table =>
                {
                    table.HasCheckConstraint(
                        "CK_Comments_Rating",
                        "[Rating] >= 1 AND [Rating] <= 5");
                });

                entity.HasKey(x => x.Id);

                entity.Property(x => x.UserId)
                    .IsRequired();

                entity.Property(x => x.Text)
                    .HasMaxLength(500)
                    .HasColumnType("nvarchar(500)")
                    .IsRequired();

                entity.Property(x => x.Rating)
                    .IsRequired();

                entity.Property(x => x.CreatedAt)
                    .IsRequired();

                entity.Property(x => x.IsApproved)
                    .IsRequired();

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new
                {
                    x.IsApproved,
                    x.CreatedAt
                });

                entity.HasIndex(x => new
                {
                    x.UserId,
                    x.CreatedAt
                });

                entity.HasIndex(x => new
                {
                    x.IsApproved,
                    x.Rating
                });
            });
        }

        private static void ConfigureCourse(ModelBuilder modelBuilder)
        {
            
        }
    }
}
