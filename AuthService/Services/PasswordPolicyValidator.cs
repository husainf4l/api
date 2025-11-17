using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace AuthService.Services;

public interface IPasswordPolicyValidator
{
    void Validate(string password, string email);
}

public class PasswordPolicyValidator : IPasswordPolicyValidator
{
    private static readonly Regex UpperCaseRegex = new("[A-Z]", RegexOptions.Compiled);
    private static readonly Regex LowerCaseRegex = new("[a-z]", RegexOptions.Compiled);
    private static readonly Regex DigitRegex = new("\\d", RegexOptions.Compiled);
    private static readonly Regex SpecialRegex = new("[^a-zA-Z0-9]", RegexOptions.Compiled);

    public void Validate(string password, string email)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("Password cannot be empty.");
        }

        if (password.Length < 12)
        {
            throw new ValidationException("Password must be at least 12 characters long.");
        }

        if (!UpperCaseRegex.IsMatch(password) ||
            !LowerCaseRegex.IsMatch(password) ||
            !DigitRegex.IsMatch(password) ||
            !SpecialRegex.IsMatch(password))
        {
            throw new ValidationException("Password must include uppercase, lowercase, number, and special characters.");
        }

        if (!string.IsNullOrEmpty(email))
        {
            var normalizedEmail = email.ToLowerInvariant();
            if (password.ToLowerInvariant().Contains(normalizedEmail))
            {
                throw new ValidationException("Password must not contain your email address.");
            }
        }
    }
}

