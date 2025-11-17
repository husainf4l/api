using Amazon.SimpleEmail;
using EmailService.Services;
using EmailService.Middleware;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Configure AWS SES
builder.Services.AddAWSService<IAmazonSimpleEmailService>();

// Register email service
builder.Services.AddScoped<IEmailService, AwsSesEmailService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Add API Key authentication middleware
app.UseMiddleware<ApiKeyAuthMiddleware>();

app.MapControllers();

app.Run();
