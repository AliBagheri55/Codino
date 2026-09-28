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

        public DbSet<User> Users { get; set; }

        public DbSet<Comment> Comments { get; set; }

        public DbSet<Course> Courses { get; set; }


        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // =========================
            // USERS
            // =========================

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

                entity.HasIndex(x => x.PhoneNumber)
                    .IsUnique();

                entity.Property(x => x.PasswordHash)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(x => x.IsPhoneVerified)
                    .IsRequired();

                entity.Property(x => x.CreatedAt)
                    .IsRequired();
            });


            // =========================
            // COMMENTS
            // =========================

            modelBuilder.Entity<Comment>(entity =>
            {
                entity.ToTable("Comments");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.UserId)
                    .IsRequired();

                entity.Property(x => x.Text)
                    .HasMaxLength(1000)
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
                    .OnDelete(DeleteBehavior.Cascade);
            });


            // =========================
            // COURSES
            // =========================
modelBuilder.Entity<Course>(entity =>
{
    entity.ToTable("Courses");

    entity.HasKey(x => x.Id);

    entity.Property(x => x.Title)
        .HasMaxLength(200)
        .IsRequired();

    entity.Property(x => x.Description)
        .HasMaxLength(1000)
        .IsRequired();

    entity.Property(x => x.Image)
        .HasMaxLength(500)
        .IsRequired();

    entity.Property(x => x.Price)
        .HasColumnType("decimal(18,2)")
        .IsRequired();

    entity.Property(x => x.Chapters)
        .IsRequired();

    entity.Property(x => x.Sessions)
        .IsRequired();

    entity.Property(x => x.IsActive)
        .IsRequired();

    entity.Property(x => x.CreatedAt)
        .IsRequired();

    entity.Property(x => x.Slug)
        .HasMaxLength(100)
        .IsRequired();

    entity.HasIndex(x => x.Slug)
        .IsUnique();
});

        }
    }
}