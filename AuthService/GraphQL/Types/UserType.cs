using AuthService.GraphQL.DataLoaders;
using AuthService.Models.Entities;
using HotChocolate.Types;

namespace AuthService.GraphQL.Types;

public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Field(t => t.Id);
        descriptor.Field(t => t.ApplicationId);
        descriptor.Field(t => t.Email);
        descriptor.Field(t => t.IsEmailVerified);
        descriptor.Field(t => t.PhoneNumber);
        descriptor.Field(t => t.TwoFactorEnabled);
        descriptor.Field(t => t.CreatedAt);
        descriptor.Field(t => t.LastLoginAt);
        descriptor.Field(t => t.IsActive);

        // Hide sensitive fields
        descriptor.Ignore(t => t.NormalizedEmail);
        descriptor.Ignore(t => t.PasswordHash);
        descriptor.Ignore(t => t.TwoFactorSecret);
        descriptor.Ignore(t => t.TwoFactorBackupCodes);

        // Configure navigation properties
        descriptor.Field(t => t.Application)
            .Resolve(ctx => ctx.DataLoader<ApplicationDataLoader>().LoadAsync(ctx.Parent<User>().ApplicationId, ctx.RequestAborted));

        descriptor.Field(t => t.UserRoles)
            .UseFiltering()
            .UseSorting()
            .Resolve(ctx => ctx.DataLoader<UserRolesDataLoader>().LoadAsync(ctx.Parent<User>().Id, ctx.RequestAborted));

        descriptor.Field(t => t.ApiKeys)
            .UseFiltering()
            .UseSorting();

        descriptor.Field(t => t.SessionLogs)
            .UseFiltering()
            .UseSorting();
    }
}
