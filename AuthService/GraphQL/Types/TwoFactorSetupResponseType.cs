using AuthService.GraphQL.Mutations;
using HotChocolate.Types;

namespace AuthService.GraphQL.Types;

public class TwoFactorSetupResponseType : ObjectType<TwoFactorSetupResponse>
{
    protected override void Configure(IObjectTypeDescriptor<TwoFactorSetupResponse> descriptor)
    {
        descriptor.Field(t => t.Success);
        descriptor.Field(t => t.Secret);
        descriptor.Field(t => t.QrCodeUri);
        descriptor.Field(t => t.QrCodeImageBase64);
        descriptor.Field(t => t.BackupCodes);
    }
}
