using WhatsApp.Services;
using WhatsApp.GraphQL;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// Register custom services
builder.Services.AddSingleton<TokenManagerService>();
builder.Services.AddSingleton<WhatsAppService>();

// Configure GraphQL
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    // Enable GraphQL IDE (Banana Cake Pop)
    app.MapGraphQL().WithOptions(new HotChocolate.AspNetCore.GraphQLServerOptions
    {
        Tool = { Enable = true }
    });
}
else
{
    app.MapGraphQL();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
