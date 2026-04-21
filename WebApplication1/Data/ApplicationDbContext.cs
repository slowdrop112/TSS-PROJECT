using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Uniflow.Models;

namespace Uniflow.Data
{
    // Moștenim din IdentityDbContext pentru a avea tabelele de useri (Identity)
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Tabele pentru Cursuri
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseEnrollment> CourseEnrollments { get; set; }
        
        // Tabele pentru cereri de roluri
        public DbSet<RoleRequest> RoleRequests { get; set; }
        
        // Tabele pentru profil utilizator
        public DbSet<UserProfile> UserProfiles { get; set; }
        
        // Tabele pentru notițe
        public DbSet<Note> Notes { get; set; }
        
        // Tabele pentru upvotes la notițe
        public DbSet<NoteVote> NoteVotes { get; set; }
        
        // Tabele pentru fișiere/materiale cursuri
        public DbSet<CourseMaterial> CourseMaterials { get; set; }
        
        // Tabele pentru partajare notițe
        public DbSet<NoteShare> NoteShares { get; set; }
        
        // Tabele pentru comentarii notițe
        public DbSet<NoteComment> NoteComments { get; set; }
        
        // Tabele pentru anunțuri cursuri
        public DbSet<CourseAnnouncement> CourseAnnouncements { get; set; }

        // Tabele pentru notificări utilizatori
        // Tabele pentru notificări utilizatori
        public DbSet<UserNotification> UserNotifications { get; set; }

        // Tabele pentru Gamification (Badges)
        public DbSet<Badge> Badges { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }

        // Tabele pentru Vouchers
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<UserVoucher> UserVouchers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurare pentru Course - adaptat la structura existentă din DB
            modelBuilder.Entity<Course>(entity =>
            {
                entity.ToTable("Courses");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("CourseID");
                
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description);
                entity.Property(e => e.Category).HasMaxLength(50);
                entity.Property(e => e.DurationHours);
                entity.Property(e => e.CreatedDate).HasColumnName("DateCreated").IsRequired();
                entity.Property(e => e.IsPublished).IsRequired().HasDefaultValue(true);
                
                // ProfesorId - ACTIVAT! Coloana există în DB
                entity.Property(e => e.ProfesorId).HasMaxLength(450);
                entity.HasOne(e => e.Profesor)
                    .WithMany()
                    .HasForeignKey(e => e.ProfesorId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);

                // Relație one-to-many cu CourseEnrollment
                entity.HasMany(e => e.Enrollments)
                    .WithOne(e => e.Course)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configurare pentru CourseEnrollment
            modelBuilder.Entity<CourseEnrollment>(entity =>
            {
                entity.ToTable("CourseEnrollments");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CourseId).IsRequired();
                entity.Property(e => e.StudentId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.EnrollmentDate).IsRequired();

                // Relație cu Course
                entity.HasOne(e => e.Course)
                    .WithMany(c => c.Enrollments)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relație cu IdentityUser (Student)
                entity.HasOne(e => e.Student)
                    .WithMany()
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Index unic pentru a preveni înscrieri duplicate
                // (un student nu poate fi înscris de două ori la același curs)
                entity.HasIndex(e => new { e.CourseId, e.StudentId })
                    .IsUnique();
            });

            // Configurare pentru RoleRequest
            modelBuilder.Entity<RoleRequest>(entity =>
            {
                entity.ToTable("RoleRequests");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.RequestedRole).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.RequestDate).IsRequired();
                entity.Property(e => e.ProcessedByUserId).HasMaxLength(450);

                // Relații
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ProcessedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ProcessedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configurare pentru UserProfile
            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.ToTable("UserProfiles");
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.XP).IsRequired().HasDefaultValue(0);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configurare pentru Note
            modelBuilder.Entity<Note>(entity =>
            {
                entity.ToTable("Notes");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.CourseId).IsRequired();
                entity.Property(e => e.StudentId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Pending");
                entity.Property(e => e.CreatedDate).IsRequired();
                entity.Property(e => e.ValidatedByUserId).HasMaxLength(450);

                entity.HasOne(e => e.Course)
                    .WithMany()
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Student)
                    .WithMany()
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ValidatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.ValidatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configurare pentru NoteVote
            modelBuilder.Entity<NoteVote>(entity =>
            {
                entity.ToTable("NoteVotes");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NoteId).IsRequired();
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.IsUpvote).IsRequired();
                entity.Property(e => e.VoteDate).IsRequired();

                entity.HasOne(e => e.Note)
                    .WithMany(n => n.Votes)
                    .HasForeignKey(e => e.NoteId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Index unic pentru a preveni voturi duplicate (un user poate vota o dată per notiță)
                entity.HasIndex(e => new { e.NoteId, e.UserId })
                    .IsUnique();
            });

            // Configurare pentru CourseMaterial
            modelBuilder.Entity<CourseMaterial>(entity =>
            {
                entity.ToTable("CourseMaterials");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.CourseId).IsRequired();
                entity.Property(e => e.UploadedByUserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.UploadDate).IsRequired();
                entity.Property(e => e.FileSize).IsRequired();
                entity.Property(e => e.ContentType).HasMaxLength(100);

                entity.HasOne(e => e.Course)
                    .WithMany()
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.UploadedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UploadedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configurare pentru UserNotification
            modelBuilder.Entity<UserNotification>(entity =>
            {
                entity.ToTable("UserNotifications");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(20).HasDefaultValue("info");
                entity.Property(e => e.IsRead).IsRequired().HasDefaultValue(false);
                entity.Property(e => e.CreatedDate).IsRequired();
                entity.Property(e => e.LinkUrl).HasMaxLength(500);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.UserId, e.IsRead });
                entity.HasIndex(e => new { e.UserId, e.CreatedDate });
            });

            // Configurare pentru NoteShare
            modelBuilder.Entity<NoteShare>(entity =>
            {
                entity.ToTable("NoteShares");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NoteId).IsRequired();
                entity.Property(e => e.OwnerId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.SharedWithUserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.SharedDate).IsRequired();

                entity.HasOne(e => e.Note)
                    .WithMany(n => n.Shares)
                    .HasForeignKey(e => e.NoteId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Owner)
                    .WithMany()
                    .HasForeignKey(e => e.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.SharedWithUser)
                    .WithMany()
                    .HasForeignKey(e => e.SharedWithUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Index unic pentru a preveni partajări duplicate
                entity.HasIndex(e => new { e.NoteId, e.SharedWithUserId })
                    .IsUnique();
            });

            // Configurare pentru NoteComment
            modelBuilder.Entity<NoteComment>(entity =>
            {
                entity.ToTable("NoteComments");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NoteId).IsRequired();
                entity.Property(e => e.AuthorId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.Content).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.CreatedDate).IsRequired();

                entity.HasOne(e => e.Note)
                    .WithMany(n => n.Comments)
                    .HasForeignKey(e => e.NoteId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Author)
                    .WithMany()
                    .HasForeignKey(e => e.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ParentComment)
                    .WithMany(c => c.Replies)
                    .HasForeignKey(e => e.ParentCommentId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Indexuri pentru performanță
                entity.HasIndex(e => e.NoteId);
                entity.HasIndex(e => e.AuthorId);
                entity.HasIndex(e => e.CreatedDate);
            });

            // Configurare pentru CourseAnnouncement
            modelBuilder.Entity<CourseAnnouncement>(entity =>
            {
                entity.ToTable("CourseAnnouncements");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.CourseId).IsRequired();
                entity.Property(e => e.PostedByUserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.PostedDate).IsRequired();
                entity.Property(e => e.IsImportant).IsRequired();

                entity.HasOne(e => e.Course)
                    .WithMany()
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.PostedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.PostedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configurare pentru UserBadge
            modelBuilder.Entity<UserBadge>(entity =>
            {
                entity.ToTable("UserBadges");
                entity.HasIndex(e => new { e.UserId, e.BadgeId }).IsUnique();
            });

            // Configurare pentru Voucher
            modelBuilder.Entity<Voucher>(entity =>
            {
                entity.ToTable("Vouchers");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.PartnerName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.DiscountType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DiscountValue).IsRequired().HasMaxLength(50);
                entity.Property(e => e.RequiredLevel).IsRequired();
                entity.Property(e => e.ValidityDays).IsRequired();
                entity.Property(e => e.IconUrl).HasMaxLength(500);
                entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            });

            // Configurare pentru UserVoucher
            modelBuilder.Entity<UserVoucher>(entity =>
            {
                entity.ToTable("UserVouchers");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.VoucherId).IsRequired();
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.AwardedDate).IsRequired();
                entity.Property(e => e.ExpiryDate).IsRequired();
                entity.Property(e => e.IsRedeemed).IsRequired().HasDefaultValue(false);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Voucher)
                    .WithMany()
                    .HasForeignKey(e => e.VoucherId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Index pentru căutare rapidă după user și status
                entity.HasIndex(e => new { e.UserId, e.IsRedeemed });
                entity.HasIndex(e => e.Code).IsUnique();
            });
        }
    }
}
