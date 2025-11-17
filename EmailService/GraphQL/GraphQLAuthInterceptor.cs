using HotChocolate.AspNetCore;
using HotChocolate.Execution;

namespace EmailService.GraphQL;

public class GraphQLAuthInterceptor : IHttpRequestInterceptor
{
    private readonly IConfiguration _configuration;

    public GraphQLAuthInterceptor(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public ValueTask OnCreateAsync(HttpContext context, IRequestExecutor requestExecutor, OperationRequestBuilder requestBuilder, CancellationToken cancellationToken)
    {
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        var expectedApiKey = _configuration["ApiSettings:ApiKey"];

        if (!string.IsNullOrEmpty(apiKey) && apiKey == expectedApiKey)
        {
            // Set user identity for authorization
            var identity = new System.Security.Claims.ClaimsIdentity("ApiKey");
            identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "ApiUser"));
            context.User = new System.Security.Claims.ClaimsPrincipal(identity);
        }

        return ValueTask.CompletedTask;
    }
}
