using AuthService.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    // DbSets for all entities
    public DbSet<Application> Applications { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<SessionLog> SessionLogs { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<EmailToken> EmailTokens { get; set; }
    public DbSet<UserExternalLogin> UserExternalLogins { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure composite primary key for UserRole
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        // Configure relationships
        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes for better performance
        modelBuilder.Entity<Application>()
            .HasIndex(a => a.Code)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => new { u.ApplicationId, u.NormalizedEmail })
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.NormalizedEmail);

        modelBuilder.Entity<Role>()
            .HasIndex(r => new { r.ApplicationId, r.Name })
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => new { rt.UserId, rt.ApplicationId });

        // Configure table names (optional, EF will pluralize automatically)
        modelBuilder.Entity<Application>().ToTable("Applications");
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<Role>().ToTable("Roles");
        modelBuilder.Entity<UserRole>().ToTable("UserRoles");
        modelBuilder.Entity<RefreshToken>().ToTable("RefreshTokens");
        modelBuilder.Entity<SessionLog>().ToTable("SessionLogs");
        modelBuilder.Entity<ApiKey>().ToTable("ApiKeys");
        modelBuilder.Entity<EmailToken>().ToTable("EmailTokens");
        modelBuilder.Entity<UserExternalLogin>().ToTable("UserExternalLogins");

        // Configure EmailToken indexes
        modelBuilder.Entity<EmailToken>()
            .HasIndex(et => et.Token)
            .IsUnique();

        modelBuilder.Entity<EmailToken>()
            .HasIndex(et => new { et.UserId, et.TokenType, et.IsUsed });

        // Configure UserExternalLogin indexes
        modelBuilder.Entity<UserExternalLogin>()
            .HasIndex(uel => new { uel.Provider, uel.ProviderUserId })
            .IsUnique();

        modelBuilder.Entity<UserExternalLogin>()
            .HasIndex(uel => uel.UserId);
    }
}
