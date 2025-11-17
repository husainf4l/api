using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthService.Models.Entities;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Services;

public class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(User user, Application application, List<string> roles)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        var issuer = jwtSettings["Issuer"] ?? "AuthService";
        var audience = jwtSettings["Audience"] ?? "AuthServiceClients";
        var accessTokenExpiryMinutes = int.Parse(jwtSettings["AccessTokenExpiryMinutes"] ?? "15");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("app", application.Code),
            new Claim("app_id", application.Id.ToString()),
            new Claim("app_name", application.Name),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddMinutes(accessTokenExpiryMinutes).ToUnixTimeSeconds().ToString())
        };

        // Add roles as claims
        foreach (var role in roles)
        {
            claims.Add(new Claim("roles", role));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(accessTokenExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
            var issuer = jwtSettings["Issuer"] ?? "AuthService";
            var audience = jwtSettings["Audience"] ?? "AuthServiceClients";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public Dictionary<string, string> ExtractClaims(string token)
    {
        var claims = new Dictionary<string, string>();
        var principal = ValidateToken(token);

        if (principal != null)
        {
            foreach (var claim in principal.Claims)
            {
                claims[claim.Type] = claim.Value;
            }
        }

        return claims;
    }

    public Guid? GetUserIdFromToken(string token)
    {
        var claims = ExtractClaims(token);
        if (claims.TryGetValue(JwtRegisteredClaimNames.Sub, out var userIdStr) &&
            Guid.TryParse(userIdStr, out var userId))
        {
            return userId;
        }
        return null;
    }

    public string? GetApplicationCodeFromToken(string token)
    {
        var claims = ExtractClaims(token);
        claims.TryGetValue("app", out var appCode);
        return appCode;
    }

    public Guid? GetApplicationIdFromToken(string token)
    {
        var claims = ExtractClaims(token);
        if (claims.TryGetValue("app_id", out var appIdStr) &&
            Guid.TryParse(appIdStr, out var appId))
        {
            return appId;
        }
        return null;
    }

    public List<string> GetRolesFromToken(string token)
    {
        var claims = ExtractClaims(token);
        return claims.Where(c => c.Key == "roles").Select(c => c.Value).ToList();
    }
}
