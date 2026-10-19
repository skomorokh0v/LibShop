using LibShop.Data;
using LibShop.Models;
using Microsoft.EntityFrameworkCore;

namespace LibShop.Services;

/// <summary>Результат попытки входа в систему.</summary>
public enum LoginResult
{
    Success,
    UserNotFound,
    WrongPassword,
    CaptchaRequired,
    WrongCaptcha,
    LockedOut
}

/// <summary>Аутентификация и регистрация пользователей.</summary>
public static class AuthService
{
    /// <summary>После скольких неверных паролей требуется капча.</summary>
    public const int CaptchaThreshold = 3;

    /// <summary>После скольких неверных паролей вход блокируется.</summary>
    public const int LockoutThreshold = 5;

    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(1);

    private sealed record AttemptState(int FailCount, DateTime? LockedUntil);

    private static readonly Dictionary<string, AttemptState> Attempts = new();
    private static readonly object Sync = new();

    /// <summary>Проверяет, требуется ли капча для логина.</summary>
    public static bool RequiresCaptcha(string login)
    {
        lock (Sync)
        {
            return Attempts.TryGetValue(login.Trim(), out AttemptState? state) &&
                   state.FailCount >= CaptchaThreshold &&
                   (state.LockedUntil == null || state.LockedUntil <= DateTime.Now);
        }
    }

    /// <summary>Оставшееся время блокировки (null — нет блокировки).</summary>
    public static TimeSpan? GetLockoutRemaining(string login)
    {
        lock (Sync)
        {
            if (Attempts.TryGetValue(login.Trim(), out AttemptState? state) &&
                state.LockedUntil is { } until && until > DateTime.Now)
            {
                return until - DateTime.Now;
            }
            return null;
        }
    }

    /// <summary>Число неудачных попыток входа подряд для логина.</summary>
    public static int GetFailCount(string login)
    {
        lock (Sync)
        {
            return Attempts.TryGetValue(login.Trim(), out AttemptState? state)
                ? state.FailCount
                : 0;
        }
    }

    /// <summary>
    /// Проверяет учётную запись. Пароль сверяется через <see cref="PasswordService"/>,
    /// после <see cref="CaptchaThreshold"/> ошибок требуется капча,
    /// после <see cref="LockoutThreshold"/> — блокировка на минуту.
    /// </summary>
    public static (LoginResult Result, User? User) Login(
        string login, string password, string? captchaInput, string? captchaCode)
    {
        login = login.Trim();

        if (RequiresCaptcha(login))
        {
            if (!CaptchaService.Verify(captchaInput, captchaCode ?? string.Empty))
                return (LoginResult.WrongCaptcha, null);
        }

        using LibShopContext db = LibShopContext.Create();
        User? user = db.Users.FirstOrDefault(u => u.Login == login);

        if (user == null)
        {
            RegisterFailedAttempt(login);
            return (LoginResult.UserNotFound, null);
        }

        if (!PasswordService.Verify(password, user.PasswordSalt, user.PasswordHash))
        {
            RegisterFailedAttempt(login);
            return (LoginResult.WrongPassword, null);
        }

        ClearAttempts(login);
        return (LoginResult.Success, user);
    }

    private static void RegisterFailedAttempt(string login)
    {
        lock (Sync)
        {
            AttemptState state = Attempts.TryGetValue(login, out AttemptState? s) ? s : new(0, null);
            int failCount = state.FailCount + 1;
            DateTime? lockedUntil =
                failCount >= LockoutThreshold ? DateTime.Now.Add(LockoutDuration) : state.LockedUntil;
            Attempts[login] = new AttemptState(failCount, lockedUntil);
        }
    }

    private static void ClearAttempts(string login)
    {
        lock (Sync)
        {
            Attempts.Remove(login);
        }
    }

    /// <summary>Проверяет, свободен ли логин.</summary>
    public static bool IsLoginAvailable(string login)
    {
        using LibShopContext db = LibShopContext.Create();
        return !db.Users.Any(u => u.Login == login.Trim());
    }

    /// <summary>Регистрирует нового пользователя. Возвращает текст ошибки или null при успехе.</summary>
    public static string? Register(string login, string password, string fullName,
        string? email, string? phone)
    {
        login = login.Trim();

        if (!IsLoginAvailable(login))
            return "Пользователь с таким логином уже существует.";

        (string salt, string hash) = PasswordService.HashNewPassword(password);

        using LibShopContext db = LibShopContext.Create();
        db.Users.Add(new User
        {
            Login = login,
            PasswordSalt = salt,
            PasswordHash = hash,
            FullName = fullName.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            Role = UserRole.User,
            CreatedAt = DateTime.Now
        });
        db.SaveChanges();

        ClearAttempts(login);
        return null;
    }

    /// <summary>Смена пароля. Возвращает текст ошибки или null при успехе.</summary>
    public static string? ChangePassword(User user, string currentPassword, string newPassword)
    {
        if (!PasswordService.Verify(currentPassword, user.PasswordSalt, user.PasswordHash))
            return "Текущий пароль указан неверно.";

        (string salt, string hash) = PasswordService.HashNewPassword(newPassword);

        using LibShopContext db = LibShopContext.Create();
        User? dbUser = db.Users.Find(user.Id);
        if (dbUser == null)
            return "Пользователь не найден в базе данных.";

        dbUser.PasswordSalt = salt;
        dbUser.PasswordHash = hash;
        db.SaveChanges();

        return null;
    }

    /// <summary>Возвращает пользователя из БД по идентификатору.</summary>
    public static User? GetUser(int id)
    {
        using LibShopContext db = LibShopContext.Create();
        return db.Users.AsNoTracking().FirstOrDefault(u => u.Id == id);
    }
}
