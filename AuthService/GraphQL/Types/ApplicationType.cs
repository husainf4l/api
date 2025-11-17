using AuthService.Models.Entities;
using HotChocolate.Types;

namespace AuthService.GraphQL.Types;

public class ApplicationType : ObjectType<Application>
{
    protected override void Configure(IObjectTypeDescriptor<Application> descriptor)
    {
        descriptor.Field(t => t.Id);
        descriptor.Field(t => t.Name);
        descriptor.Field(t => t.Code);
        descriptor.Field(t => t.ClientId);
        descriptor.Field(t => t.CreatedAt);
        descriptor.Field(t => t.IsActive);

        // Hide sensitive fields
        descriptor.Ignore(t => t.ClientSecretHash);

        // Configure navigation properties
        descriptor.Field(t => t.Users)
            .UseFiltering()
            .UseSorting();

        descriptor.Field(t => t.Roles)
            .UseFiltering()
            .UseSorting();

        descriptor.Field(t => t.ApiKeys)
            .UseFiltering()
            .UseSorting();

        descriptor.Field(t => t.SessionLogs)
            .UseFiltering()
            .UseSorting();
    }
}
