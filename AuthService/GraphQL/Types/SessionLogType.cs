using AuthService.GraphQL.DataLoaders;
using AuthService.Models.Entities;
using HotChocolate.Types;

namespace AuthService.GraphQL.Types;

public class SessionLogType : ObjectType<SessionLog>
{
    protected override void Configure(IObjectTypeDescriptor<SessionLog> descriptor)
    {
        descriptor.Field(t => t.Id);
        descriptor.Field(t => t.UserId);
        descriptor.Field(t => t.ApplicationId);
        descriptor.Field(t => t.LoginAt);
        descriptor.Field(t => t.LogoutAt);
        descriptor.Field(t => t.IpAddress);
        descriptor.Field(t => t.UserAgent);
        descriptor.Field(t => t.IsSuccessful);

        // Configure navigation properties
        descriptor.Field(t => t.User)
            .Resolve(ctx => ctx.DataLoader<UserDataLoader>().LoadAsync(ctx.Parent<SessionLog>().UserId, ctx.RequestAborted));

        descriptor.Field(t => t.Application)
            .Resolve(ctx => ctx.DataLoader<ApplicationDataLoader>().LoadAsync(ctx.Parent<SessionLog>().ApplicationId, ctx.RequestAborted));
    }
}
