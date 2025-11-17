using AuthService.Models.Entities;
using HotChocolate.Types;

namespace AuthService.GraphQL.Types;

public class RoleType : ObjectType<Role>
{
    protected override void Configure(IObjectTypeDescriptor<Role> descriptor)
    {
        descriptor.Field(t => t.Id);
        descriptor.Field(t => t.ApplicationId);
        descriptor.Field(t => t.Name);
        descriptor.Field(t => t.Description);

        // Configure navigation properties
        descriptor.Field(t => t.Application)
            .Resolve(ctx => ctx.DataLoader<ApplicationDataLoader>().LoadAsync(ctx.Parent<Role>().ApplicationId, ctx.RequestAborted));

        descriptor.Field(t => t.UserRoles)
            .UseFiltering()
            .UseSorting();
    }
}
