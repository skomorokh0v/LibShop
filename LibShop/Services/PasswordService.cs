using System.Security.Cryptography;

namespace LibShop.Services;

/// <summary>Хэширование и проверка паролей (PBKDF2, SHA-256).</summary>
public static class PasswordService
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    /// <summary>Создаёт случайную соль.</summary>
    public static string GenerateSalt()
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        return Convert.ToBase64String(salt);
    }

    /// <summary>Вычисляет хэш пароля с указанной солью (base64).</summary>
    public static string Hash(string password, string saltBase64)
    {
        byte[] salt = Convert.FromBase64String(saltBase64);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return Convert.ToBase64String(hash);
    }

    /// <summary>Создаёт соль и хэш для нового пароля.</summary>
    public static (string Salt, string Hash) HashNewPassword(string password)
    {
        string salt = GenerateSalt();
        return (salt, Hash(password, salt));
    }

    /// <summary>Проверяет пароль против сохранённого хэша.</summary>
    public static bool Verify(string password, string saltBase64, string expectedHashBase64)
    {
        byte[] expected = Convert.FromBase64String(expectedHashBase64);
        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
            password, Convert.FromBase64String(saltBase64),
            Iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
