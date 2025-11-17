using AuthService.GraphQL.Queries;
using HotChocolate.Types;

namespace AuthService.GraphQL.Types;

public class ApplicationStatsType : ObjectType<ApplicationStats>
{
    protected override void Configure(IObjectTypeDescriptor<ApplicationStats> descriptor)
    {
        descriptor.Field(t => t.ApplicationId);
        descriptor.Field(t => t.UserCount);
        descriptor.Field(t => t.ApiKeyCount);
        descriptor.Field(t => t.RoleCount);
        descriptor.Field(t => t.SessionCount);
    }
}
