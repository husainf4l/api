using AuthService.GraphQL.Mutations;
using HotChocolate.Types;

namespace AuthService.GraphQL.Types;

public class CreateApiKeyResponseType : ObjectType<CreateApiKeyResponse>
{
    protected override void Configure(IObjectTypeDescriptor<CreateApiKeyResponse> descriptor)
    {
        descriptor.Field(t => t.Id);
        descriptor.Field(t => t.Name);
        descriptor.Field(t => t.ApiKey);
        descriptor.Field(t => t.Scopes);
        descriptor.Field(t => t.ExpiresAt);
        descriptor.Field(t => t.CreatedAt);
    }
}
