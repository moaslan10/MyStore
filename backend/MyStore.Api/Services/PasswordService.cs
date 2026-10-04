using System.Security.Cryptography;

namespace MyStore.Api.Services;

public static class PasswordService
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 120_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"PBKDF2-SHA256:{Iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(key)}";
    }

    public static bool Verify(string password, string hash)
    {
        try
        {
            var parts = hash.Split(':');
            if (parts.Length != 4 || parts[0] != "PBKDF2-SHA256") return false;
            var iterations = int.Parse(parts[1]);
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch { return false; }
    }

    public static string HashToken(string token)
    {
        return Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
    }
}
