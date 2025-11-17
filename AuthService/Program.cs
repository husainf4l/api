using AuthService.Data;
using AuthService.Services;
using AuthService.GraphQL;
using AuthService.GraphQL.DataLoaders;
using AuthService.GraphQL.Types;
using AuthService.GraphQL.Queries;
using AuthService.GraphQL.Mutations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Services = AuthService.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure logging to reduce noise
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.DataProtection", LogLevel.Warning);

// Add services to the container
builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddType<ApplicationType>()
    .AddType<UserType>()
    .AddType<RegisterRequestInput>()
    .AddType<LoginRequestInput>()
    .AddType<TokenResponseType>()
    .AddType<UserInfoType>()
    .AddFiltering()
    .AddSorting()
    .AddProjections();

// Configure Entity Framework with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Register DbContext using the recommended pattern for both regular DI and DataLoaders
builder.Services.AddDbContextFactory<AuthDbContext>(options =>
    options.UseNpgsql(connectionString)
           .ConfigureWarnings(warnings => 
               warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// Add scoped DbContext that uses the factory (for services that need it)
builder.Services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<AuthDbContext>>().CreateDbContext());

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

// Configure Data Protection - store keys in app directory
var keysPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "DataProtection-Keys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("AuthService");

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
    .AddCheck<DbContextHealthCheck>("database")
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

// Register data loaders for GraphQL
builder.Services.AddScoped<ApplicationDataLoader>();
builder.Services.AddScoped<UserDataLoader>();

// Register email service - use real GraphQL email service for testing
builder.Services.AddScoped<IEmailService, EmailService>();

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
    // GraphQL endpoint is available at /auth/graphql
    // You can use tools like GraphQL Playground, Altair, or Insomnia to test
}

// HTTPS is handled by reverse proxy (nginx), so we don't need HTTPS redirection

// Enable CORS
app.UseCors("AllowAll");

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Map GraphQL endpoint
app.MapGraphQL();

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
