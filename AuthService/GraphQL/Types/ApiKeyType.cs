using AuthService.Models.Entities;
using HotChocolate.Types;

namespace AuthService.GraphQL.Types;

public class ApiKeyType : ObjectType<ApiKey>
{
    protected override void Configure(IObjectTypeDescriptor<ApiKey> descriptor)
    {
        descriptor.Field(t => t.Id);
        descriptor.Field(t => t.ApplicationId);
        descriptor.Field(t => t.OwnerUserId);
        descriptor.Field(t => t.Name);
        descriptor.Field(t => t.Scope);
        descriptor.Field(t => t.CreatedAt);
        descriptor.Field(t => t.ExpiresAt);
        descriptor.Field(t => t.RevokedAt);
        descriptor.Field(t => t.LastUsedAt);
        descriptor.Field(t => t.IsActive);
        descriptor.Field(t => t.IsRevoked);

        // Hide sensitive fields
        descriptor.Ignore(t => t.HashedKey);

        // Configure navigation properties
        descriptor.Field(t => t.Application)
            .Resolve(ctx => ctx.DataLoader<ApplicationDataLoader>().LoadAsync(ctx.Parent<ApiKey>().ApplicationId, ctx.RequestAborted));

        descriptor.Field(t => t.OwnerUser)
            .Resolve(ctx => ctx.DataLoader<UserDataLoader>().LoadAsync(ctx.Parent<ApiKey>().OwnerUserId, ctx.RequestAborted));
    }
}
