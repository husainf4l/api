using System.Text;
using AuthService.Data;
using AuthService.Middleware;
using AuthService.Repositories;
using AuthService.Services;
using AuthService.Services.Background;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using DotNetEnv;

// Load environment variables from .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();

// Configure Database
var connectionString = $"Host={Environment.GetEnvironmentVariable("DATABASE_HOST")};" +
                      $"Port={Environment.GetEnvironmentVariable("DATABASE_PORT")};" +
                      $"Database={Environment.GetEnvironmentVariable("DATABASE_NAME")};" +
                      $"Username={Environment.GetEnvironmentVariable("DATABASE_USER")};" +
                      $"Password={Environment.GetEnvironmentVariable("DATABASE_PASSWORD")}";

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !isDevelopment;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Register Services
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ICorsOriginRepository, CorsOriginRepository>();
builder.Services.AddScoped<IPasswordHistoryRepository, PasswordHistoryRepository>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();
builder.Services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAuthService, AuthService.Services.AuthService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddSingleton<ICorsPolicyProvider, DatabaseCorsPolicyProvider>();
builder.Services.AddSingleton<IRefreshTokenHasher, Sha256RefreshTokenHasher>();
builder.Services.AddSingleton<IRateLimiter, InMemoryRateLimiter>();
builder.Services.AddSingleton<IPasswordPolicyValidator, PasswordPolicyValidator>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IAuditLogger, SiemAuditLogger>();
builder.Services.AddScoped<ISecurityJob, RefreshTokenCleanupJob>();
builder.Services.AddScoped<ISecurityJob, DormantAccountReviewJob>();
builder.Services.AddHostedService<SecurityJobScheduler>();

// Add Controllers
builder.Services.AddControllers();

// Add Razor Pages for Admin Dashboard
builder.Services.AddRazorPages();

builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
    options.HttpsPort = 443;
});

// Configure CORS service (policy resolved from database provider)
builder.Services.AddCors();

// Swagger disabled due to .NET 10 compatibility issues
// Use /admin dashboard instead

var app = builder.Build();

// Configure the HTTP request pipeline
if (isDevelopment)
{
    // Swagger disabled - use /admin dashboard
    app.UseDeveloperExceptionPage();
}

if (!isDevelopment)
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Enable CORS with dynamic policy
app.UseCors(CorsPolicyNames.DynamicCors);

// API Key Validation Middleware (before JWT)
app.UseApiKeyValidation();

// Custom JWT Middleware
app.UseMiddleware<JwtMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Serve static files for dashboard
app.UseStaticFiles();

app.MapControllers();
app.MapRazorPages();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck");

app.Run();
