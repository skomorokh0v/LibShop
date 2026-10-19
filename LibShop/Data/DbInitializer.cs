using LibShop.Models;
using LibShop.Services;
using Microsoft.EntityFrameworkCore;

namespace LibShop.Data;

/// <summary>Создание базы данных и начальное заполнение.</summary>
public static class DbInitializer
{
    /// <summary>Создаёт БД при отсутствии и заполняет тестовыми данными.</summary>
    public static void Initialize()
    {
        using LibShopContext db = LibShopContext.Create();
        db.Database.EnsureCreated();

        if (db.Users.Any())
            return; // БД уже заполнена.

        Seed(db);
        db.SaveChanges();
    }

    private static void Seed(LibShopContext db)
    {
        // --- Пользователи ---
        (string adminSalt, string adminHash) = PasswordService.HashNewPassword("Admin123!");
        (string userSalt, string userHash) = PasswordService.HashNewPassword("User12345");

        var admin = new User
        {
            Login = "admin",
            PasswordSalt = adminSalt,
            PasswordHash = adminHash,
            FullName = "Иванов Иван Иванович",
            Email = "admin@libshop.ru",
            Phone = "+79990001122",
            Role = UserRole.Admin,
            CreatedAt = DateTime.Now.AddMonths(-6)
        };

        var user = new User
        {
            Login = "user",
            PasswordSalt = userSalt,
            PasswordHash = userHash,
            FullName = "Петров Пётр Петрович",
            Email = "user@libshop.ru",
            Phone = "+79990003344",
            Role = UserRole.User,
            CreatedAt = DateTime.Now.AddMonths(-3)
        };

        db.Users.AddRange(admin, user);

        // --- Книги ---
        var books = new List<Book>
        {
            new() { Title = "Преступление и наказание", Author = "Ф.М. Достоевский", Genre = "Классика", Publisher = "Азбука", PublicationYear = 1866, Price = 590, Quantity = 12, Description = "Роман о нравственных страданиях бывшего студента Раскольникова." },
            new() { Title = "Мастер и Маргарита", Author = "М.А. Булгаков", Genre = "Классика", Publisher = "Азбука", PublicationYear = 1967, Price = 650, Quantity = 8, Description = "Роман о визите дьявола в Москву 1930-х годов." },
            new() { Title = "Война и мир", Author = "Л.Н. Толстой", Genre = "Классика", Publisher = "Эксмо", PublicationYear = 1869, Price = 890, Quantity = 5, Description = "Эпопея о судьбах русского дворянства в эпоху наполеоновских войн." },
            new() { Title = "1984", Author = "Дж. Оруэлл", Genre = "Антиутопия", Publisher = "АСТ", PublicationYear = 1949, Price = 450, Quantity = 15, Description = "Классическая антиутопия о тоталитарном обществе." },
            new() { Title = "Маленький принц", Author = "А. де Сент-Экзюпери", Genre = "Философская проза", Publisher = "Азбука", PublicationYear = 1943, Price = 380, Quantity = 20, Description = "Притча о дружбе, любви и ответственности." },
            new() { Title = "Гарри Поттер и философский камень", Author = "Дж. Роулинг", Genre = "Фэнтези", Publisher = "Махаон", PublicationYear = 1997, Price = 720, Quantity = 10, Description = "Первая книга о юном волшебнике." },
            new() { Title = "Властелин колец. Братство кольца", Author = "Дж.Р.Р. Толкин", Genre = "Фэнтези", Publisher = "АСТ", PublicationYear = 1954, Price = 780, Quantity = 7, Description = "Эпическое фэнтези о пути Хранителей Кольца." },
            new() { Title = "Дюна", Author = "Ф. Герберт", Genre = "Научная фантастика", Publisher = "АСТ", PublicationYear = 1965, Price = 850, Quantity = 6, Description = "Монументальная сага о пустынной планете Арракис." },
            new() { Title = "Основание", Author = "А. Азимов", Genre = "Научная фантастика", Publisher = "АСТ", PublicationYear = 1951, Price = 620, Quantity = 9, Description = "Цикл романов о гибели и возрождении Галактической империи." },
            new() { Title = "Три товарища", Author = "Э.М. Ремарк", Genre = "Зарубежная классика", Publisher = "Азбука", PublicationYear = 1936, Price = 560, Quantity = 11, Description = "История дружбы и любви в Германии 1920-х." },
            new() { Title = "Убийство в Восточном экспрессе", Author = "А. Кристи", Genre = "Детектив", Publisher = "Эксмо", PublicationYear = 1934, Price = 430, Quantity = 14, Description = "Классический детектив с Эркюлем Пуаро." },
            new() { Title = "Шерлок Холмс. Этюд в багровых тонах", Author = "А. Конан Дойл", Genre = "Детектив", Publisher = "Азбука", PublicationYear = 1887, Price = 410, Quantity = 13, Description = "Первое дело великого сыщика." },
            new() { Title = "Алгоритмы: построение и анализ", Author = "Т. Кормен", Genre = "Программирование", Publisher = "Вильямс", PublicationYear = 2006, Price = 2400, Quantity = 3, Description = "Классический справочник по алгоритмам." },
            new() { Title = "Чистый код", Author = "Р. Мартин", Genre = "Программирование", Publisher = "Питер", PublicationYear = 2008, Price = 1350, Quantity = 6, Description = "Руководство по созданию понятного и надёжного кода." },
            new() { Title = "Sapiens. Краткая история человечества", Author = "Ю. Ной", Genre = "Нон-фикшн", Publisher = "АСТ", PublicationYear = 2011, Price = 900, Quantity = 9, Description = "История человечества от каменного века до наших дней." },
        };

        db.Books.AddRange(books);

        // --- Заказы (для отчётов и диаграммы) ---
        DateTime now = DateTime.Now;
        var orders = new List<Order>
        {
            new() { User = user, Book = books[0], Quantity = 1, UnitPrice = books[0].Price, TotalPrice = books[0].Price, OrderDate = now.AddDays(-40), Status = OrderStatus.Paid },
            new() { User = user, Book = books[3], Quantity = 1, UnitPrice = books[3].Price, TotalPrice = books[3].Price, OrderDate = now.AddDays(-32), Status = OrderStatus.Paid },
            new() { User = user, Book = books[7], Quantity = 2, UnitPrice = books[7].Price, TotalPrice = books[7].Price * 2, OrderDate = now.AddDays(-25), Status = OrderStatus.Paid },
            new() { User = user, Book = books[10], Quantity = 1, UnitPrice = books[10].Price, TotalPrice = books[10].Price, OrderDate = now.AddDays(-14), Status = OrderStatus.Cancelled },
            new() { User = user, Book = books[12], Quantity = 1, UnitPrice = books[12].Price, TotalPrice = books[12].Price, OrderDate = now.AddDays(-5), Status = OrderStatus.Paid },
            new() { User = user, Book = books[5], Quantity = 1, UnitPrice = books[5].Price, TotalPrice = books[5].Price, OrderDate = now.AddDays(-1), Status = OrderStatus.Pending },
            new() { User = admin, Book = books[1], Quantity = 1, UnitPrice = books[1].Price, TotalPrice = books[1].Price, OrderDate = now.AddDays(-20), Status = OrderStatus.Paid },
            new() { User = admin, Book = books[8], Quantity = 3, UnitPrice = books[8].Price, TotalPrice = books[8].Price * 3, OrderDate = now.AddDays(-9), Status = OrderStatus.Paid },
        };

        db.Orders.AddRange(orders);
    }
}

