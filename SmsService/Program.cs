using SmsService.Services;
using SmsService.Repositories;
using SmsService.GraphQL.Queries;
using SmsService.GraphQL.Mutations;
using SmsService.GraphQL.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddHttpContextAccessor();

// Register SMS repository
builder.Services.AddScoped<SmsMessageRepository>();

// Register HttpClient for SMS service
builder.Services.AddHttpClient<ISmsService, JosmsSmsService>();

// Register SMS service
builder.Services.AddScoped<ISmsService, JosmsSmsService>();

// Add GraphQL
builder.Services
    .AddGraphQLServer()
    .AddQueryType<SmsQueries>()
    .AddMutationType<SmsMutations>()
    .UseField<ApiKeyAuthorizationMiddleware>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseHttpsRedirection();

// Map GraphQL endpoint
app.MapGraphQL("/graphql");

app.Run();
