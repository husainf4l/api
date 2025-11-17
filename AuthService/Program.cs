using AuthService.Data;
using AuthService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Services = AuthService.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure logging to reduce noise
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.DataProtection", LogLevel.Warning);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Entity Framework with PostgreSQL
builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.ConfigureWarnings(warnings => 
        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not configured"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"Token validated for user: {context.Principal?.Identity?.Name}");
            return Task.CompletedTask;
        }
    };
});

// Add authorization
builder.Services.AddAuthorization();

// Configure Data Protection - keys will persist in mounted volume
var keysDirectory = new DirectoryInfo("/root/.aspnet/DataProtection-Keys");
if (!keysDirectory.Exists)
{
    keysDirectory.Create();
}

// Configure OAuth providers
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["OAuth:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["OAuth:Google:ClientSecret"] ?? "";
        options.CallbackPath = "/auth/external-callback/google";

        // Request additional scopes
        options.Scope.Add("email");
        options.Scope.Add("profile");

        // Save tokens for refresh
        options.SaveTokens = true;

        options.Events.OnCreatingTicket = async context =>
        {
            // Store additional user info from Google
            var picture = context.User.GetProperty("picture").GetString();
            var locale = context.User.GetProperty("locale").GetString();

            // You can store this in claims or database as needed
            context.Identity?.AddClaim(new System.Security.Claims.Claim("picture", picture ?? ""));
            context.Identity?.AddClaim(new System.Security.Claims.Claim("locale", locale ?? ""));
        };
    })
    .AddOAuth("GitHub", options =>
    {
        options.ClientId = builder.Configuration["OAuth:GitHub:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["OAuth:GitHub:ClientSecret"] ?? "";
        options.CallbackPath = "/auth/external-callback/github";
        options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
        options.TokenEndpoint = "https://github.com/login/oauth/access_token";
        options.UserInformationEndpoint = "https://api.github.com/user";
        options.SaveTokens = true;

        options.Scope.Add("user:email");
        options.Scope.Add("read:user");

        // Note: GitHub OAuth will work with default claim mapping
        // Additional claim mapping can be added if needed
    })
    .AddOAuth("Apple", options =>
    {
        options.ClientId = builder.Configuration["OAuth:Apple:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["OAuth:Apple:ClientSecret"] ?? "";
        options.CallbackPath = "/auth/external-callback/apple";
        options.AuthorizationEndpoint = "https://appleid.apple.com/auth/authorize";
        options.TokenEndpoint = "https://appleid.apple.com/auth/token";
        options.UserInformationEndpoint = "https://appleid.apple.com/auth/userinfo";

        // Apple uses JWT client authentication, but for simplicity we'll use client_secret
        // In production, you'd generate JWT tokens for client authentication
        options.SaveTokens = true;

        options.Scope.Add("email");
        options.Scope.Add("name");

        // Note: Apple Sign-In requires additional configuration for JWT client auth
        // This is a simplified implementation - production should use proper JWT client auth
    });

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AuthDbContext>("database")
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

// Register application services
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<ApplicationService>();
builder.Services.AddScoped<Services.AuthService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ApiKeyService>();
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();
builder.Services.AddScoped<IExternalLoginService, ExternalLoginService>();

// Add HttpClient for email service
builder.Services.AddHttpClient();

// Register email service (use console service in development, real service in production)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IEmailService, ConsoleEmailService>();
}
else
{
    builder.Services.AddScoped<IEmailService, EmailService>();
}

// Add Razor Pages for dashboard (if needed)
builder.Services.AddRazorPages();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Use path base for all routes (adds /auth prefix)
app.UsePathBase("/auth");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/auth/swagger/v1/swagger.json", "AuthService API V1");
    });
}

// HTTPS is handled by reverse proxy (nginx), so we don't need HTTPS redirection

// Enable CORS
app.UseCors("AllowAll");

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map Razor Pages (for dashboard)
app.MapRazorPages();

// Map health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = (check) => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = (check) => check.Tags.Contains("live")
});

// Database migration (run on startup)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Apply any pending migrations
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed: {Message}", ex.Message);
        // Don't throw in production, let the app start and handle it gracefully
    }
}

app.Run();
