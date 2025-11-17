using AuthService.Models.DTOs;
using HotChocolate.Types;
using System.ComponentModel.DataAnnotations;

namespace AuthService.GraphQL.Types;

// Input types for mutations
public class RegisterRequestInput : InputObjectType<RegisterRequest>
{
    protected override void Configure(IInputObjectTypeDescriptor<RegisterRequest> descriptor)
    {
        descriptor.Field(t => t.Email)
            .Type<NonNullType<StringType>>()
            .Description("User email address");

        descriptor.Field(t => t.Password)
            .Type<NonNullType<StringType>>()
            .Description("User password (minimum 8 characters)");

        descriptor.Field(t => t.ConfirmPassword)
            .Type<NonNullType<StringType>>()
            .Description("Password confirmation");

        descriptor.Field(t => t.ApplicationCode)
            .Type<NonNullType<StringType>>()
            .Description("Application code for registration");

        descriptor.Field(t => t.PhoneNumber)
            .Description("Optional phone number");

        descriptor.Field(t => t.FirstName)
            .Description("Optional first name");

        descriptor.Field(t => t.LastName)
            .Description("Optional last name");
    }
}

public class LoginRequestInput : InputObjectType<LoginRequest>
{
    protected override void Configure(IInputObjectTypeDescriptor<LoginRequest> descriptor)
    {
        descriptor.Field(t => t.Email)
            .Type<NonNullType<StringType>>()
            .Description("User email address");

        descriptor.Field(t => t.Password)
            .Type<NonNullType<StringType>>()
            .Description("User password");

        descriptor.Field(t => t.ApplicationCode)
            .Type<NonNullType<StringType>>()
            .Description("Application code");

        descriptor.Field(t => t.DeviceInfo)
            .Description("Optional device information");

        descriptor.Field(t => t.IpAddress)
            .Description("Optional IP address");

        descriptor.Field(t => t.UserAgent)
            .Description("Optional user agent");
    }
}

public class CreateApplicationRequestInput : InputObjectType<CreateApplicationRequest>
{
    protected override void Configure(IInputObjectTypeDescriptor<CreateApplicationRequest> descriptor)
    {
        descriptor.Field(t => t.Name)
            .Type<NonNullType<StringType>>()
            .Description("Application name");

        descriptor.Field(t => t.Code)
            .Type<NonNullType<StringType>>()
            .Description("Unique application code (lowercase, no spaces)");

        descriptor.Field(t => t.Description)
            .Description("Optional application description");
    }
}

public class CreateApiKeyRequestInput : InputObjectType<CreateApiKeyRequest>
{
    protected override void Configure(IInputObjectTypeDescriptor<CreateApiKeyRequest> descriptor)
    {
        descriptor.Field(t => t.Name)
            .Type<NonNullType<StringType>>()
            .Description("API key name");

        descriptor.Field(t => t.Description)
            .Description("Optional API key description");

        descriptor.Field(t => t.Scopes)
            .Type<NonNullType<ListType<NonNullType<StringType>>>>()
            .Description("List of scopes for the API key");

        descriptor.Field(t => t.Environment)
            .Type<NonNullType<StringType>>()
            .Description("Environment (development, staging, production)");

        descriptor.Field(t => t.ExpiresInDays)
            .Description("Optional expiration in days");

        descriptor.Field(t => t.RateLimitPerHour)
            .Description("Optional rate limit per hour");
    }
}

public class ValidateApiKeyRequestInput : InputObjectType<ValidateApiKeyRequest>
{
    protected override void Configure(IInputObjectTypeDescriptor<ValidateApiKeyRequest> descriptor)
    {
        descriptor.Field(t => t.ApiKey)
            .Type<NonNullType<StringType>>()
            .Description("API key to validate");

        descriptor.Field(t => t.RequestedScope)
            .Description("Optional requested scope");

        descriptor.Field(t => t.IpAddress)
            .Description("Optional IP address");

        descriptor.Field(t => t.UserAgent)
            .Description("Optional user agent");
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
        descriptor.Field(t => t.Role);
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
