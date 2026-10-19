# LibShop — WPF-приложение книжного магазина (учебная практика УП.01.01)

## Описание решения
Реализовано WPF-приложение для учёта платежей пользователей книжного магазина с подключённой базой данных Microsoft SQL Server (сервер **HOME-PC**, база **LibShop**).

### Используемый стек
- .NET 10, WPF
- Entity Framework Core (SqlServer)
- MaterialDesignInXamlToolkit 5.3.2 (Material Design)
- DocumentFormat.OpenXml (.xlsx, .docx)
- QuestPDF (.pdf)

### Структура решения
- `LibShop/` — WPF-проект
  - `Models/` — сущности (User, Book, Order)
  - `Data/` — контекст БД (LibShopContext), инициализатор (DbInitializer)
  - `Services/` — сервисы: AuthService (авторизация/регистрация/защита от брутфорса), PasswordService (PBKDF2), CaptchaService, MessageService, ThemeService, ExportService
  - `Pages/` — страницы (Авторизация, Регистрация, Пользователь, Администратор, Смена пароля) и окна редактирования
  - `Resources/AppStyles.xaml` — собственный словарь стилей
  - `App.xaml` — Material Design тема + словарь стилей
- `db/schema.sql` — скрипт создания БД с таблицами Users, Books, Orders и заполнением тестовыми данными
- `res/` — ресурсы (скриншоты приложения)

## Учётные записи по умолчанию
- `admin / Admin123!` — администратор
- `user / User12345` — пользователь

## Требования к окружению
- Microsoft SQL Server на сервере `HOME-PC` (Windows-аутентификация)
- .NET 10 SDK
