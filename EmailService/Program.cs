using Amazon.SimpleEmail;
using EmailService.Services;
using EmailService.Data;
using EmailService.GraphQL;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;

// Load environment variables from .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add environment variables to configuration
builder.Configuration.AddEnvironmentVariables();

// Add environment variables to configuration
builder.Configuration.AddEnvironmentVariables();

// Add services to the container
builder.Services.AddGraphQLServer()
    .AddQueryType<EmailQueries>()
    .AddMutationType<EmailMutations>()
    .AddHttpRequestInterceptor<GraphQLAuthInterceptor>()
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = builder.Environment.IsDevelopment());

// Configure PostgreSQL Database
builder.Services.AddDbContext<EmailDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure AWS SES
builder.Services.AddAWSService<IAmazonSimpleEmailService>();

// Register email service
builder.Services.AddScoped<IEmailService, AwsSesEmailService>();

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiKey", policy =>
        policy.RequireAssertion(context =>
        {
            var httpContext = context.Resource as HttpContext;
            if (httpContext == null) return false;

            var apiKey = httpContext.Request.Headers["X-API-Key"].FirstOrDefault();
            var expectedApiKey = builder.Configuration["ApiSettings:ApiKey"];
            return !string.IsNullOrEmpty(apiKey) && apiKey == expectedApiKey;
        }));
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.MapGraphQL();

app.Run();
