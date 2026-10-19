-- ============================================================
-- LibShop — база данных книжного магазина
-- СУБД: Microsoft SQL Server (сервер HOME-PC, Windows-аутентификация)
-- Таблицы: Users, Books, Orders (3 таблицы, каскадные связи)
-- ============================================================

IF DB_ID(N'LibShop') IS NULL
    CREATE DATABASE LibShop;
GO

USE LibShop;
GO

-- ------------------------------------------------------------
-- Пользователи
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id            INT             IDENTITY(1,1) NOT NULL,
        Login         NVARCHAR(20)    NOT NULL,
        PasswordHash  NVARCHAR(88)    NOT NULL,
        PasswordSalt  NVARCHAR(44)    NOT NULL,
        FullName      NVARCHAR(100)   NOT NULL,
        Email         NVARCHAR(100)   NULL,
        Phone         NVARCHAR(20)    NULL,
        Role          NVARCHAR(10)    NOT NULL DEFAULT N'User',
        CreatedAt     DATETIME2       NOT NULL DEFAULT SYSDATETIME(),

        CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Users_Login UNIQUE (Login),
        CONSTRAINT CK_Users_Role CHECK (Role IN (N'User', N'Admin'))
    );
END
GO

-- ------------------------------------------------------------
-- Книги
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.Books', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Books
    (
        Id              INT             IDENTITY(1,1) NOT NULL,
        Title           NVARCHAR(200)   NOT NULL,
        Author          NVARCHAR(100)   NOT NULL,
        Genre           NVARCHAR(50)    NOT NULL,
        Publisher       NVARCHAR(100)   NULL,
        PublicationYear INT             NOT NULL,
        Price           DECIMAL(10,2)   NOT NULL,
        Quantity        INT             NOT NULL,
        Description     NVARCHAR(1000)  NULL,

        CONSTRAINT PK_Books PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Books_Year CHECK (PublicationYear BETWEEN 1000 AND 2100),
        CONSTRAINT CK_Books_Price CHECK (Price >= 0),
        CONSTRAINT CK_Books_Quantity CHECK (Quantity >= 0)
    );
END
GO

-- ------------------------------------------------------------
-- Заказы (платежи). Каскадное удаление: при удалении пользователя
-- или книги связанные заказы удаляются автоматически.
-- ------------------------------------------------------------
IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders
    (
        Id         INT            IDENTITY(1,1) NOT NULL,
        UserId     INT            NOT NULL,
        BookId     INT            NOT NULL,
        Quantity   INT            NOT NULL,
        UnitPrice  DECIMAL(10,2)  NOT NULL,
        TotalPrice DECIMAL(10,2)  NOT NULL,
        OrderDate  DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
        Status     NVARCHAR(10)   NOT NULL DEFAULT N'Pending',

        CONSTRAINT PK_Orders PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Orders_Quantity CHECK (Quantity > 0),
        CONSTRAINT CK_Orders_Status CHECK (Status IN (N'Pending', N'Paid', N'Cancelled')),
        CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId)
            REFERENCES dbo.Users (Id) ON DELETE CASCADE,
        CONSTRAINT FK_Orders_Books FOREIGN KEY (BookId)
            REFERENCES dbo.Books (Id) ON DELETE CASCADE
    );
END
GO

-- ============================================================
-- Заполнение таблиц (выполняется только при пустой БД)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM dbo.Users)
BEGIN
    -- Пароли: PBKDF2 (SHA-256, 100 000 итераций), соль и хэш в base64.
    -- admin / Admin123!    user / User12345
    INSERT INTO dbo.Users (Login, PasswordHash, PasswordSalt, FullName, Email, Phone, Role, CreatedAt)
    VALUES
        (N'admin', N'b+gfklVBIOUOJ3QgsfXT2mWxyNMnXC1vC1khSp8xYTQ=', N'MTIzNDU2Nzg5MGFiY2Rl',
         N'Иванов Иван Иванович', N'admin@libshop.ru', N'+79990001122', N'Admin', DATEADD(MONTH, -6, SYSDATETIME())),
        (N'user',  N'DYIHMSNGvPP/XEzG7zuxELQVvullJOubKnL6n36sjQQ=', N'YWJjZGVmZ2hpamtsbW5vcA==',
         N'Петров Пётр Петрович', N'user@libshop.ru', N'+79990003344', N'User', DATEADD(MONTH, -3, SYSDATETIME()));
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Books)
BEGIN
    INSERT INTO dbo.Books (Title, Author, Genre, Publisher, PublicationYear, Price, Quantity, Description)
    VALUES
        (N'Преступление и наказание', N'Ф.М. Достоевский', N'Классика', N'Азбука', 1866, 590.00, 12, N'Роман о нравственных страданиях бывшего студента Раскольникова.'),
        (N'Мастер и Маргарита', N'М.А. Булгаков', N'Классика', N'Азбука', 1967, 650.00, 8, N'Роман о визите дьявола в Москву 1930-х годов.'),
        (N'Война и мир', N'Л.Н. Толстой', N'Классика', N'Эксмо', 1869, 890.00, 5, N'Эпопея о судьбах русского дворянства в эпоху наполеоновских войн.'),
        (N'1984', N'Дж. Оруэлл', N'Антиутопия', N'АСТ', 1949, 450.00, 15, N'Классическая антиутопия о тоталитарном обществе.'),
        (N'Маленький принц', N'А. де Сент-Экзюпери', N'Философская проза', N'Азбука', 1943, 380.00, 20, N'Притча о дружбе, любви и ответственности.'),
        (N'Гарри Поттер и философский камень', N'Дж. Роулинг', N'Фэнтези', N'Махаон', 1997, 720.00, 10, N'Первая книга о юном волшебнике.'),
        (N'Властелин колец. Братство кольца', N'Дж.Р.Р. Толкин', N'Фэнтези', N'АСТ', 1954, 780.00, 7, N'Эпическое фэнтези о пути Хранителей Кольца.'),
        (N'Дюна', N'Ф. Герберт', N'Научная фантастика', N'АСТ', 1965, 850.00, 6, N'Монументальная сага о пустынной планете Арракис.'),
        (N'Основание', N'А. Азимов', N'Научная фантастика', N'АСТ', 1951, 620.00, 9, N'Цикл романов о гибели и возрождении Галактической империи.'),
        (N'Три товарища', N'Э.М. Ремарк', N'Зарубежная классика', N'Азбука', 1936, 560.00, 11, N'История дружбы и любви в Германии 1920-х.'),
        (N'Убийство в Восточном экспрессе', N'А. Кристи', N'Детектив', N'Эксмо', 1934, 430.00, 14, N'Классический детектив с Эркюлем Пуаро.'),
        (N'Шерлок Холмс. Этюд в багровых тонах', N'А. Конан Дойл', N'Детектив', N'Азбука', 1887, 410.00, 13, N'Первое дело великого сыщика.'),
        (N'Алгоритмы: построение и анализ', N'Т. Кормен', N'Программирование', N'Вильямс', 2006, 2400.00, 3, N'Классический справочник по алгоритмам.'),
        (N'Чистый код', N'Р. Мартин', N'Программирование', N'Питер', 2008, 1350.00, 6, N'Руководство по созданию понятного и надёжного кода.'),
        (N'Sapiens. Краткая история человечества', N'Ю. Ной', N'Нон-фикшн', N'АСТ', 2011, 900.00, 9, N'История человечества от каменного века до наших дней.');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Orders)
BEGIN
    DECLARE @now DATETIME2 = SYSDATETIME();

    INSERT INTO dbo.Orders (UserId, BookId, Quantity, UnitPrice, TotalPrice, OrderDate, Status)
    SELECT u.Id, b.Id, v.Quantity, b.Price, b.Price * v.Quantity, DATEADD(DAY, -v.DaysAgo, @now), v.Status
    FROM (VALUES
        (N'user',  N'Преступление и наказание',        1, 40, N'Paid'),
        (N'user',  N'1984',                            1, 32, N'Paid'),
        (N'user',  N'Дюна',                            2, 25, N'Paid'),
        (N'user',  N'Убийство в Восточном экспрессе',  1, 14, N'Cancelled'),
        (N'user',  N'Алгоритмы: построение и анализ',  1,  5, N'Paid'),
        (N'user',  N'Гарри Поттер и философский камень',1,  1, N'Pending'),
        (N'admin', N'Мастер и Маргарита',              1, 20, N'Paid'),
        (N'admin', N'Основание',                       3,  9, N'Paid')
    ) AS v (Login, Title, Quantity, DaysAgo, Status)
    JOIN dbo.Users u ON u.Login = v.Login
    JOIN dbo.Books b ON b.Title = v.Title;
END
GO
