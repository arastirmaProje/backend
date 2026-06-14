using Microsoft.EntityFrameworkCore;
using Personelim.Models;

namespace Personelim.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Business> Businesses { get; set; }
        public DbSet<BusinessMember> BusinessMembers { get; set; }
        public DbSet<Invitation> Invitations { get; set; }
        public DbSet<Province> Provinces { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<MemberDocument> MemberDocuments { get; set; }
        public DbSet<MemberLeave> MemberLeaves { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<PerformanceReport> PerformanceReports { get; set; }
        
        public DbSet<BusinessDocument> BusinessDocuments { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<SlackWebhook> SlackWebhooks { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<ChatConversation> ChatConversations { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<DepartmentPerformanceReport> DepartmentPerformanceReports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ChatConversation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ChatType).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.HasIndex(e => new { e.UserId, e.IsActive });

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Business)
                    .WithMany()
                    .HasForeignKey(e => e.BusinessId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Department)
                    .WithMany()
                    .HasForeignKey(e => e.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.IslemYapildi).HasMaxLength(100);
                entity.HasIndex(e => new { e.ConversationId, e.CreatedAt });

                entity.HasOne(e => e.Conversation)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(e => e.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DepartmentPerformanceReport>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DepartmanAdi).IsRequired().HasMaxLength(200);
                entity.Property(e => e.AiRequestJson).IsRequired();
                entity.Property(e => e.AiResponseJson).IsRequired();
                entity.HasIndex(e => new { e.BusinessId, e.DepartmentId, e.PeriodEnd });

                entity.HasOne(e => e.Business)
                    .WithMany()
                    .HasForeignKey(e => e.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Department)
                    .WithMany()
                    .HasForeignKey(e => e.DepartmentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email)
                    .IsUnique()
                    .HasFilter("\"IsActive\" = true");
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
            });
            
            modelBuilder.Entity<Business>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
                entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Description).HasMaxLength(1000);
                
                entity.HasOne(b => b.Owner)
                    .WithMany(u => u.OwnedBusinesses)
                    .HasForeignKey(b => b.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(b => b.Province)
                    .WithMany()
                    .HasForeignKey(b => b.ProvinceId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(b => b.District)
                    .WithMany()
                    .HasForeignKey(b => b.DistrictId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(b => b.ParentBusiness)
                    .WithMany(b => b.SubBusinesses)
                    .HasForeignKey(b => b.ParentBusinessId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);
            });
            
            modelBuilder.Entity<Schedule>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(20);

                entity.HasOne(e => e.Business)
                    .WithMany()
                    .HasForeignKey(e => e.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SlackWebhook>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.WebhookUrl).IsRequired().HasMaxLength(500);
                entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Label).HasMaxLength(100);

                entity.HasOne(e => e.Business)
                    .WithMany()
                    .HasForeignKey(e => e.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(100);

                entity.HasOne(e => e.Business)
                    .WithMany()
                    .HasForeignKey(e => e.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BusinessMember>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Position).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => new { e.UserId, e.BusinessId }).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany(u => u.BusinessMemberships)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Business)
                    .WithMany(b => b.Members)
                    .HasForeignKey(e => e.BusinessId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Department)
                    .WithMany(d => d.Members)
                    .HasForeignKey(e => e.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
            });
            
            modelBuilder.Entity<Invitation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.InvitationCode).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);

                entity.HasOne(e => e.Business)
                    .WithMany(b => b.Invitations)
                    .HasForeignKey(e => e.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.InvitedBy)
                    .WithMany()
                    .HasForeignKey(e => e.InvitedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            
            modelBuilder.Entity<Province>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            });
            
            modelBuilder.Entity<District>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);

                entity.HasOne(d => d.Province)
                    .WithMany(p => p.Districts)
                    .HasForeignKey(d => d.ProvinceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            
            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).IsRequired().HasMaxLength(6);
                entity.Property(e => e.ExpiresAt).IsRequired();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}