namespace LibShop.Models;

/// <summary>Роль пользователя в системе.</summary>
public enum UserRole
{
    User,
    Admin
}

/// <summary>Пользователь системы (покупатель или администратор).</summary>
public class User
{
    public int Id { get; set; }

    /// <summary>Логин (уникален, латиница, цифры, "_", 3–20 символов).</summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>Хэш пароля (PBKDF2, base64).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Соль хэша пароля (base64).</summary>
    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>Фамилия и имя.</summary>
    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public UserRole Role { get; set; } = UserRole.User;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Человекочитаемое описание роли (для отображения в интерфейсе).</summary>
    public string RoleDescription => Role == UserRole.Admin ? "Администратор" : "Пользователь";

    /// <summary>Заказы пользователя (каскадное удаление).</summary>
    public List<Order> Orders { get; set; } = new();
}
