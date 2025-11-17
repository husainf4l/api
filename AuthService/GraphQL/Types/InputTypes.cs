using AuthService.Models.DTOs;
using HotChocolate.Types;

namespace AuthService.GraphQL.Types;

// Input types for mutations
public class RegisterRequestInput : InputObjectType<RegisterRequest>
{
    protected override void Configure(IInputObjectTypeDescriptor<RegisterRequest> descriptor)
    {
        descriptor.Field(t => t.Email);
        descriptor.Field(t => t.Password);
        descriptor.Field(t => t.ConfirmPassword);
        descriptor.Field(t => t.ApplicationCode);
        descriptor.Field(t => t.PhoneNumber);
        descriptor.Field(t => t.FirstName);
        descriptor.Field(t => t.LastName);
    }
}

public class LoginRequestInput : InputObjectType<LoginRequest>
{
    protected override void Configure(IInputObjectTypeDescriptor<LoginRequest> descriptor)
    {
        descriptor.Field(t => t.Email);
        descriptor.Field(t => t.Password);
        descriptor.Field(t => t.ApplicationCode);
        descriptor.Field(t => t.DeviceInfo);
        descriptor.Field(t => t.IpAddress);
        descriptor.Field(t => t.UserAgent);
    }
}

public class CreateApplicationRequestInput : InputObjectType<CreateApplicationRequest>
{
    protected override void Configure(IInputObjectTypeDescriptor<CreateApplicationRequest> descriptor)
    {
        descriptor.Field(t => t.Name);
        descriptor.Field(t => t.Code);
        descriptor.Field(t => t.Description);
    }
}

public class CreateApiKeyRequestInput : InputObjectType<CreateApiKeyRequest>
{
    protected override void Configure(IInputObjectTypeDescriptor<CreateApiKeyRequest> descriptor)
    {
        descriptor.Field(t => t.Name);
        descriptor.Field(t => t.Description);
        descriptor.Field(t => t.Scopes);
        descriptor.Field(t => t.Environment);
        descriptor.Field(t => t.ExpiresInDays);
        descriptor.Field(t => t.RateLimitPerHour);
    }
}

public class ValidateApiKeyRequestInput : InputObjectType<ValidateApiKeyRequest>
{
    protected override void Configure(IInputObjectTypeDescriptor<ValidateApiKeyRequest> descriptor)
    {
        descriptor.Field(t => t.ApiKey);
        descriptor.Field(t => t.RequestedScope);
        descriptor.Field(t => t.IpAddress);
        descriptor.Field(t => t.UserAgent);
    }
}

// Output types for responses
public class TokenResponseType : ObjectType<TokenResponse>
{
    protected override void Configure(IObjectTypeDescriptor<TokenResponse> descriptor)
    {
        descriptor.Field(t => t.AccessToken);
        descriptor.Field(t => t.RefreshToken);
        descriptor.Field(t => t.TokenType);
        descriptor.Field(t => t.ExpiresIn);
        descriptor.Field(t => t.ExpiresAt);
        descriptor.Field(t => t.User);
    }
}

public class UserInfoType : ObjectType<TokenResponse.UserInfo>
{
    protected override void Configure(IObjectTypeDescriptor<TokenResponse.UserInfo> descriptor)
    {
        descriptor.Field(t => t.Id);
        descriptor.Field(t => t.Email);
        descriptor.Field(t => t.ApplicationId);
        descriptor.Field(t => t.ApplicationCode);
        descriptor.Field(t => t.ApplicationName);
        descriptor.Field(t => t.Roles);
        descriptor.Field(t => t.IsEmailVerified);
        descriptor.Field(t => t.CreatedAt);
        descriptor.Field(t => t.LastLoginAt);
    }
}

public class ValidateApiKeyResponseType : ObjectType<ValidateApiKeyResponse>
{
    protected override void Configure(IObjectTypeDescriptor<ValidateApiKeyResponse> descriptor)
    {
        descriptor.Field(t => t.IsValid);
        descriptor.Field(t => t.Message);
        descriptor.Field(t => t.ApplicationId);
        descriptor.Field(t => t.UserId);
        descriptor.Field(t => t.Scopes);
    }
}
