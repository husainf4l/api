using Microsoft.EntityFrameworkCore;
using EmailService.Models;

namespace EmailService.Data
{
    public class EmailDbContext : DbContext
    {
        public EmailDbContext(DbContextOptions<EmailDbContext> options) : base(options)
        {
        }

        public DbSet<EmailLog> EmailLogs { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<ApiKeyModel> ApiKeys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // EmailLog configuration
            modelBuilder.Entity<EmailLog>(entity =>
            {
                entity.ToTable("email_logs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.To).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Body).IsRequired();
                entity.Property(e => e.SentAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.SentAt);
                entity.HasIndex(e => e.To);
            });

            // EmailTemplate configuration
            modelBuilder.Entity<EmailTemplate>(entity =>
            {
                entity.ToTable("email_templates");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Body).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // ApiKey configuration
            modelBuilder.Entity<ApiKeyModel>(entity =>
            {
                entity.ToTable("api_keys");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Key).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.Key).IsUnique();
            });
        }
    }
}
