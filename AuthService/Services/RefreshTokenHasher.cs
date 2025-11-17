using System.Security.Cryptography;
using System.Text;

namespace AuthService.Services;

public interface IRefreshTokenHasher
{
    string Hash(string token);
}

public class Sha256RefreshTokenHasher : IRefreshTokenHasher
{
    public string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}

