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

        // =========================================================
        // DATABASE TABLES
        // =========================================================

        public DbSet<User> Users => Set<User>();
        public DbSet<Announcement> Announcements =>Set<Announcement>();
        public DbSet<Comment> Comments => Set<Comment>();

        public DbSet<ProjectRequest> ProjectRequests =>
            Set<ProjectRequest>();

        public DbSet<CollaborationRequest> CollaborationRequests =>
            Set<CollaborationRequest>();

        // تیکت‌های پشتیبانی
        public DbSet<Ticket> Tickets => Set<Ticket>();

        // پیام‌های تیکت‌های پشتیبانی
        public DbSet<TicketMessage> TicketMessages =>
            Set<TicketMessage>();

        // =========================================================
        // MODEL CONFIGURATION
        // =========================================================

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            ConfigureAnnouncement(modelBuilder);
            ConfigureUser(modelBuilder);
            ConfigureComment(modelBuilder);
            ConfigureCourse(modelBuilder);
            ConfigureProjectRequest(modelBuilder);
            ConfigureCollaborationRequest(modelBuilder);
            ConfigureTicket(modelBuilder);
            ConfigureTicketMessage(modelBuilder);
        }

        // =========================================================
        // TICKET CONFIGURATION
        // =========================================================
        // =========================================================
        // ANNOUNCEMENT CONFIGURATION
        // =========================================================

        private static void ConfigureAnnouncement(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Announcement>(entity =>
            {
                entity.ToTable("Announcements");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Title)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.Content)
                    .HasMaxLength(10000)
                    .IsRequired();

                entity.Property(x => x.AuthorLabel)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.IsPublished)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.Property(x => x.CreatedAtUtc)
                    .IsRequired();

                entity.Property(x => x.UpdatedAtUtc)
                    .IsRequired(false);

                entity.HasIndex(x => new
                {
                    x.IsPublished,
                    x.CreatedAtUtc
                });
            });
        }
        private static void ConfigureTicket(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.ToTable("Tickets");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.TicketNumber)
                    .HasMaxLength(20)
                    .IsRequired();

                // شماره هر تیکت باید یکتا باشد.
                entity.HasIndex(x => x.TicketNumber)
                    .IsUnique();

                entity.Property(x => x.UserId)
                    .IsRequired();

                entity.Property(x => x.Subject)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.Category)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Priority)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(x => x.CreatedAtUtc)
                    .IsRequired();

                entity.Property(x => x.UpdatedAtUtc)
                    .IsRequired(false);

                entity.Property(x => x.LastMessageAtUtc)
                    .IsRequired(false);

                // ارتباط تیکت با کاربر صاحب آن
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // ارتباط یک تیکت با چندین پیام
                entity.HasMany(x => x.Messages)
                    .WithOne(x => x.Ticket)
                    .HasForeignKey(x => x.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);

                // ایندکس برای دریافت تیکت‌های هر کاربر
                entity.HasIndex(x => new
                {
                    x.UserId,
                    x.CreatedAtUtc
                });

                // ایندکس برای فیلتر بر اساس وضعیت
                entity.HasIndex(x => new
                {
                    x.Status,
                    x.CreatedAtUtc
                });
            });
        }

        // =========================================================
        // TICKET MESSAGE CONFIGURATION
        // =========================================================

        private static void ConfigureTicketMessage(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TicketMessage>(entity =>
            {
                entity.ToTable("TicketMessages");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.TicketId)
                    .IsRequired();

                entity.Property(x => x.SenderUserId)
                    .IsRequired();

                entity.Property(x => x.IsAdmin)
                    .IsRequired();

                entity.Property(x => x.Message)
                    .HasMaxLength(5000)
                    .IsRequired();

                entity.Property(x => x.CreatedAtUtc)
                    .IsRequired();

                // ارتباط پیام با کاربر ارسال‌کننده
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.SenderUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // ایندکس برای دریافت پیام‌های یک تیکت
                entity.HasIndex(x => new
                {
                    x.TicketId,
                    x.CreatedAtUtc
                });

                // ایندکس برای بررسی پیام‌های ارسال‌شده توسط کاربران
                entity.HasIndex(x => new
                {
                    x.SenderUserId,
                    x.CreatedAtUtc
                });
            });
        }

        // =========================================================
        // USER CONFIGURATION
        // =========================================================

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

                // نقش پیش‌فرض کاربران
                entity.Property(x => x.Role)
                    .HasMaxLength(20)
                    .IsRequired()
                    .HasDefaultValue("User");

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

        // =========================================================
        // COMMENT CONFIGURATION
        // =========================================================

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

        // =========================================================
        // COURSE CONFIGURATION
        // =========================================================

        private static void ConfigureCourse(ModelBuilder modelBuilder)
        {
        }

        // =========================================================
        // PROJECT REQUEST CONFIGURATION
        // =========================================================

        private static void ConfigureProjectRequest(
            ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProjectRequest>(entity =>
            {
                entity.ToTable("ProjectRequests");

                entity.HasKey(x => x.Id);

                // پروژه می‌تواند متعلق به کاربر یا مهمان باشد.
                entity.Property(x => x.UserId)
                    .IsRequired(false);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new
                {
                    x.UserId,
                    x.CreatedAtUtc
                });

                entity.Property(x => x.CustomerName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Email)
                    .HasMaxLength(254)
                    .IsRequired();

                entity.Property(x => x.PhoneNumber)
                    .HasMaxLength(11)
                    .IsRequired();

                entity.Property(x => x.ProjectName)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.ProjectType)
                    .HasMaxLength(80)
                    .IsRequired();

                entity.Property(x => x.Description)
                    .HasMaxLength(5000)
                    .IsRequired();

                entity.Property(x => x.Budget)
                    .HasMaxLength(80)
                    .IsRequired(false);

                entity.Property(x => x.Status)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(x => x.CreatedAtUtc)
                    .IsRequired();
            });
        }

        // =========================================================
        // COLLABORATION REQUEST CONFIGURATION
        // =========================================================

        private static void ConfigureCollaborationRequest(
            ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CollaborationRequest>(entity =>
            {
                entity.ToTable("CollaborationRequests");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.UserId)
                    .IsRequired();

                entity.Property(x => x.Specialty)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Experience)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Skills)
                    .HasMaxLength(300)
                    .IsRequired();

                entity.Property(x => x.PortfolioUrl)
                    .HasMaxLength(500)
                    .IsRequired(false);

                entity.Property(x => x.Description)
                    .HasMaxLength(1500)
                    .IsRequired();

                entity.Property(x => x.Status)
                    .HasMaxLength(30)
                    .IsRequired()
                    .HasDefaultValue("Pending");

                entity.Property(x => x.CreatedAt)
                    .IsRequired();

                // ارتباط درخواست همکاری با کاربر
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new
                {
                    x.UserId,
                    x.CreatedAt
                });

                entity.HasIndex(x => new
                {
                    x.Status,
                    x.CreatedAt
                });
            });
        }
    }
}
