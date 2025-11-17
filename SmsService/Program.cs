using SmsService.Middleware;
using SmsService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Register HttpClient for SMS service
builder.Services.AddHttpClient<ISmsService, JosmsSmsService>();

// Register SMS service
builder.Services.AddScoped<ISmsService, JosmsSmsService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Add API Key authentication middleware
app.UseMiddleware<ApiKeyAuthMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
