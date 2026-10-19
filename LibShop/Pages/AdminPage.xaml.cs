using System.Windows;
using System.Windows.Controls;
using LibShop.Data;
using LibShop.Models;
using LibShop.Services;
using Microsoft.EntityFrameworkCore;

namespace LibShop.Pages
{
    /// <summary>Страница администратора: управление книгами, пользователями, заказами,
    /// диаграмма и экспорт отчётов.</summary>
    public partial class AdminPage : Page
    {
        private readonly MainWindow _window;
        private List<Book> _books = new();
        private List<User> _users = new();
        private List<Order> _orders = new();

        /// <summary>Запись для построения диаграммы.</summary>
        public sealed record ChartBar(string Label, double BarHeight, string ValueLabel);

        private const string ChartSalesByMonth = "Продажи по месяцам (₽)";
        private const string ChartOrdersByStatus = "Заказы по статусам (шт.)";
        private const string ChartTopBooks = "Топ-5 книг по выручке (₽)";

        public AdminPage(MainWindow window)
        {
            _window = window;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (BookGenreComboBox.Items.Count == 0)
            {
                BookGenreComboBox.Items.Add("Все жанры");
                foreach (string s in new[]
                {
                    "По названию (А→Я)", "По цене (сначала дешёвые)",
                    "По цене (сначала дорогие)", "По году (новые первыми)"
                })
                    BookSortComboBox.Items.Add(s);
                foreach (string s in new[] { "Все роли", "Пользователь", "Администратор" })
                    UserRoleComboBox.Items.Add(s);
                foreach (string s in new[] { "Все статусы", "Ожидает оплаты", "Оплачен", "Отменён" })
                    OrderStatusComboBox.Items.Add(s);
                foreach (string s in new[] { ChartSalesByMonth, ChartOrdersByStatus, ChartTopBooks })
                    ChartModeComboBox.Items.Add(s);
                foreach (string s in new[]
                {
                    "По логину (А→Я)", "По ФИО (А→Я)", "По роли", "По дате регистрации"
                })
                    UserSortComboBox.Items.Add(s);
                foreach (string s in new[]
                {
                    "По дате (новые первыми)", "По дате (старые первыми)",
                    "По сумме (сначала большие)", "По пользователю"
                })
                    OrderSortComboBox.Items.Add(s);

                BookGenreComboBox.SelectedIndex = 0;
                BookSortComboBox.SelectedIndex = 0;
                UserRoleComboBox.SelectedIndex = 0;
                OrderStatusComboBox.SelectedIndex = 0;
                UserSortComboBox.SelectedIndex = 0;
                OrderSortComboBox.SelectedIndex = 0;
                ChartModeComboBox.SelectedIndex = 0;
            }

            ReloadBooks();
            ReloadUsers();
            ReloadOrders();
            UpdateChart();
        }

        // ==================== Книги ====================

        private void ReloadBooks()
        {
            try
            {
                using LibShopContext db = LibShopContext.Create();
                _books = db.Books.AsNoTracking().ToList();

                string selectedGenre = BookGenreComboBox.SelectedItem as string ?? "Все жанры";
                var genres = _books.Select(b => b.Genre).Distinct().OrderBy(g => g).ToList();
                BookGenreComboBox.Items.Clear();
                BookGenreComboBox.Items.Add("Все жанры");
                foreach (string g in genres)
                    BookGenreComboBox.Items.Add(g);
                BookGenreComboBox.SelectedItem = genres.Contains(selectedGenre)
                    ? selectedGenre : "Все жанры";

                ApplyBookFilters();
            }
            catch (Exception ex)
            {
                MessageService.Error($"Не удалось загрузить книги:\n{ex.Message}");
            }
        }

        private void ApplyBookFilters()
        {
            IEnumerable<Book> query = _books;

            string search = BookSearchTextBox.Text.Trim();
            if (search.Length > 0)
                query = query.Where(b =>
                    b.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    b.Author.Contains(search, StringComparison.OrdinalIgnoreCase));

            if (BookGenreComboBox.SelectedItem is string genre && genre != "Все жанры")
                query = query.Where(b => b.Genre == genre);

            query = (BookSortComboBox.SelectedItem as string) switch
            {
                "По цене (сначала дешёвые)" => query.OrderBy(b => b.Price),
                "По цене (сначала дорогие)" => query.OrderByDescending(b => b.Price),
                "По году (новые первыми)" => query.OrderByDescending(b => b.PublicationYear),
                _ => query.OrderBy(b => b.Title)
            };

            BooksGrid.ItemsSource = query.ToList();
        }

        private void BookFilters_Changed(object sender, RoutedEventArgs e)
        {
            if (IsLoaded && BooksGrid != null)
                ApplyBookFilters();
        }

        private void BookResetFilters_Click(object sender, RoutedEventArgs e)
        {
            BookSearchTextBox.Clear();
            BookGenreComboBox.SelectedIndex = 0;
            BookSortComboBox.SelectedIndex = 0;
        }

        private void RefreshBooks_Click(object sender, RoutedEventArgs e) => ReloadBooks();

        private void AddBookButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new BookEditWindow();
            if (dialog.ShowDialog() != true || dialog.Result == null)
                return;

            try
            {
                using LibShopContext db = LibShopContext.Create();
                db.Books.Add(dialog.Result);
                db.SaveChanges();
                MessageService.Info("Книга добавлена.");
                ReloadBooks();
                UpdateChart();
            }
            catch (Exception ex)
            {
                MessageService.Error($"Не удалось добавить книгу:\n{ex.Message}");
            }
        }

        private void EditBookButton_Click(object sender, RoutedEventArgs e)
        {
            if (BooksGrid.SelectedItem is not Book book)
            {
                MessageService.Warning("Выберите книгу для редактирования.");
                return;
            }

            var dialog = new BookEditWindow(book);
            if (dialog.ShowDialog() != true || dialog.Result == null)
                return;

            try
            {
                using LibShopContext db = LibShopContext.Create();
                Book? dbBook = db.Books.Find(book.Id);
                if (dbBook == null)
                {
                    MessageService.Error("Книга не найдена в базе данных.");
                    return;
                }

                dbBook.Title = dialog.Result.Title;
                dbBook.Author = dialog.Result.Author;
                dbBook.Genre = dialog.Result.Genre;
                dbBook.Publisher = dialog.Result.Publisher;
                dbBook.PublicationYear = dialog.Result.PublicationYear;
                dbBook.Price = dialog.Result.Price;
                dbBook.Quantity = dialog.Result.Quantity;
                dbBook.Description = dialog.Result.Description;
                db.SaveChanges();

                MessageService.Info("Изменения сохранены.");
                ReloadBooks();
                UpdateChart();
            }
            catch (Exception ex)
            {
                MessageService.Error($"Не удалось сохранить изменения:\n{ex.Message}");
            }
        }

        private void DeleteBookButton_Click(object sender, RoutedEventArgs e)
        {
            if (BooksGrid.SelectedItem is not Book book)
            {
                MessageService.Warning("Выберите книгу для удаления.");
                return;
            }

            if (!MessageService.Confirm(
                    $"Удалить книгу «{book.Title}»?\n\n" +
                    "Связанные заказы будут удалены каскадно.",
                    "Подтверждение удаления"))
                return;

            try
            {
                using LibShopContext db = LibShopContext.Create();
                Book? dbBook = db.Books.Find(book.Id);
                if (dbBook != null)
                {
                    db.Books.Remove(dbBook);
                    db.SaveChanges();
                }

                MessageService.Info("Книга удалена.");
                ReloadBooks();
                ReloadOrders();
                UpdateChart();
            }
            catch (Exception ex)
            {
                MessageService.Error($"Не удалось удалить книгу:\n{ex.Message}");
            }
        }

        // ==================== Пользователи ====================

        private void ReloadUsers()
        {
            try
            {
                using LibShopContext db = LibShopContext.Create();
                _users = db.Users.AsNoTracking().ToList();
                ApplyUserFilters();
            }
            catch (Exception ex)
            {
                MessageService.Error($"Не удалось загрузить пользователей:\n{ex.Message}");
            }
        }

        private void ApplyUserFilters()
        {
            IEnumerable<User> query = _users;

            string search = UserSearchTextBox.Text.Trim();
            if (search.Length > 0)
                query = query.Where(u =>
                    u.Login.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase));

            query = (UserRoleComboBox.SelectedItem as string) switch
            {
                "Пользователь" => query.Where(u => u.Role == UserRole.User),
                "Администратор" => query.Where(u => u.Role == UserRole.Admin),
                _ => query
            };

            query = (UserSortComboBox.SelectedItem as string) switch
            {
                "По ФИО (А→Я)" => query.OrderBy(u => u.FullName),
                "По роли" => query.OrderByDescending(u => u.Role).ThenBy(u => u.Login),
                "По дате регистрации" => query.OrderBy(u => u.CreatedAt),
                _ => query.OrderBy(u => u.Login)
            };

            UsersGrid.ItemsSource = query.ToList();
        }

        private void UserFilters_Changed(object sender, RoutedEventArgs e)
        {
            if (IsLoaded && UsersGrid != null)
                ApplyUserFilters();
        }

        private void UserResetFilters_Click(object sender, RoutedEventArgs e)
        {
            UserSearchTextBox.Clear();
            UserRoleComboBox.SelectedIndex = 0;
            UserSortComboBox.SelectedIndex = 0;
        }

        private void RefreshUsers_Click(object sender, RoutedEventArgs e) => ReloadUsers();

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new UserEditWindow();
            if (dialog.ShowDialog() != true || dialog.Login == null)
                return;

            try
            {
                string? error = AuthService.Register(dialog.Login, dialog.Password!,
                    dialog.FullName!, dialog.Email, dialog.Phone);
                if (error != null)
                {
                    MessageService.Error(error);
                    return;
                }

                // Назначение роли (Register всегда создаёт пользователя с ролью User).
                using LibShopContext db = LibShopContext.Create();
                User? created = db.Users.FirstOrDefault(u => u.Login == dialog.Login);
                if (created != null && dialog.Role == UserRole.Admin)
                {
                    created.Role = UserRole.Admin;
                    db.SaveChanges();
                }

                MessageService.Info("Пользователь добавлен.");
                ReloadUsers();
            }
            catch (Exception ex)
            {
                MessageService.Error($"Не удалось добавить пользователя:\n{ex.Message}");
            }
        }

        private void EditUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is not User user)
            {
                MessageService.Warning("Выберите пользователя для редактирования.");
                return;
            }

            var dialog = new UserEditWindow(user);
            if (dialog.ShowDialog() != true || dialog.Login == null)
                return;

            try
            {
                using LibShopContext db = LibShopContext.Create();
                User? dbUser = db.Users.Find(user.Id);
                if (dbUser == null)
                {
                    MessageService.Error("Пользователь не найден в базе данных.");
                    return;
                }

                // Логин должен оставаться уникальным.
                if (dialog.Login != user.Login &&
                    db.Users.Any(u => u.Login == dialog.Login))
                {
                    MessageService.Error("Пользователь с таким логином уже существует.");
                    return;
                }

                dbUser.Login = dialog.Login;
                dbUser.FullName = dialog.FullName!;
                dbUser.Email = dialog.Email;
                dbUser.Phone = dialog.Phone;
                dbUser.Role = dialog.Role;

                // Смена пароля — только если введено новое значение.
                if (!string.IsNullOrEmpty(dialog.Password))
                {
                    (string salt, string hash) =
                        PasswordService.HashNewPassword(dialog.Password!);
                    dbUser.PasswordSalt = salt;
                    dbUser.PasswordHash = hash;
                }

                db.SaveChanges();
                MessageService.Info("Изменения сохранены.");
                ReloadUsers();
                ReloadOrders();
            }
            catch (Exception ex)
            {
                MessageService.Error($"Не удалось сохранить изменения:\n{ex.Message}");
            }
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsersGrid.SelectedItem is not User user)
            {
                MessageService.Warning("Выберите пользователя для удаления.");
                return;
            }

            if (user.Id == _window.CurrentUser?.Id)
            {
                MessageService.Warning("Нельзя удалить собственную учётную запись.");
                return;
            }

            if (!MessageService.Confirm(
                    $"Удалить пользователя «{user.Login}»?\n\n" +
                    "Его заказы будут удалены каскадно.",
                    "Подтверждение удаления"))
                return;

            try
            {
                using LibShopContext db = LibShopContext.Create();
                User? dbUser = db.Users.Find(user.Id);
                if (dbUser != null)
                {
                    // Возврат книг по не-отменённым заказам пользователя на склад.
                    foreach (var dbOrder in db.Orders.Where(o => o.UserId == dbUser.Id &&
                                                             o.Status != OrderStatus.Cancelled)
                                                             .ToList())
                    {
                        if (db.Books.Find(dbOrder.BookId) is { } book)
                            book.Quantity += dbOrder.Quantity;
                        db.Orders.Remove(dbOrder);
                    }

                    db.Users.Remove(dbUser);
                    db.SaveChanges();
                }

                MessageService.Info("Пользователь удалён.");
                ReloadUsers();
                ReloadOrders();
                ReloadBooks();
                UpdateChart();
            }
            catch (Exception ex)
            {
                MessageService.Error($"Не удалось удалить пользователя:\n{ex.Message}");
            }
        }

        // ==================== Заказы ====================

        private void ReloadOrders()
        {
            try
            {
                using LibShopContext db = LibShopContext.Create();
                _orders = db.Orders.AsNoTracking()
                    .Include(o => o.User)
                    .Include(o => o.Book)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                ApplyOrderFilters();
            }
            catch (Exception ex)
            {
                MessageService.Error($"Не удалось загрузить заказы:\n{ex.Message}");
            }
        }

        private void ApplyOrderFilters()
        {
            IEnumerable<Order> query = _orders;

            string search = OrderSearchTextBox.Text.Trim();
            if (search.Length > 0)
                query = query.Where(o =>
                    (o.User != null && o.User.Login.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (o.Book != null && o.Book.Title.Contains(search, StringComparison.OrdinalIgnoreCase)));

            query = (OrderStatusComboBox.SelectedItem as string) switch
            {
                "Ожидает оплаты" => query.Where(o => o.Status == OrderStatus.Pending),
                "Оплачен" => query.Where(o => o.Status == OrderStatus.Paid),
                "Отменён" => query.Where(o => o.Status == OrderStatus.Cancelled),
                _ => query
            };

            query = (OrderSortComboBox.SelectedItem as string) switch
            {
                "По дате (старые первыми)" => query.OrderBy(o => o.OrderDate),
                "По сумме (сначала большие)" => query.OrderByDescending(o => o.TotalPrice),
                "По пользователю" => query.OrderBy(o => o.User?.Login),
                _ => query.OrderByDescending(o => o.OrderDate)
            };

            OrdersGrid.ItemsSource = query.ToList();
        }

        private void OrderFilters_Changed(object sender, RoutedEventArgs e)
        {
            if (IsLoaded && OrdersGrid != null)
                ApplyOrderFilters();
        }

        private void OrderResetFilters_Click(object sender, RoutedEventArgs e)
        {
            OrderSearchTextBox.Clear();
            OrderStatusComboBox.SelectedIndex = 0;
            OrderSortComboBox.SelectedIndex = 0;
        }

        private void RefreshOrders_Click(object sender, RoutedEventArgs e) => ReloadOrders();

        private void PayOrderButton_Click(object sender, RoutedEventArgs e) =>
            ChangeOrderStatus(OrderStatus.Paid);

        private void CancelOrderButton_Click(object sender, RoutedEventArgs e) =>
            ChangeOrderStatus(OrderStatus.Cancelled);

        private void ChangeOrderStatus(OrderStatus newStatus)
        {
            if (OrdersGrid.SelectedItem is not Order order)
            {
                MessageService.Warning("Выберите заказ.");
                return;
            }

            if (order.Status == newStatus)
            {
                MessageService.Warning("У заказа уже установлен этот статус.");
                return;
            }

            string action = newStatus == OrderStatus.Paid ? "оплатить" : "отменить";
            if (!MessageService.Confirm(
                    $"Вы действительно хотите {action} заказ №{order.Id} " +
                    $"на сумму {order.TotalPrice:F2} ₽?",
                    "Подтверждение действия"))
                return;

            try
            {
                using LibShopContext db = LibShopContext.Create();
                Order? dbOrder = db.Orders.Find(order.Id);
                if (dbOrder == null)
                {
                    MessageService.Error("Заказ не найден в базе данных.");
                    return;
                }

                // При отмене любого не-Cancelled заказа экземпляры возвращаются на склад.
                if (newStatus == OrderStatus.Cancelled &&
                    dbOrder.Status != OrderStatus.Cancelled &&
                    db.Books.Find(dbOrder.BookId) is { } book)
                {
                    book.Quantity += dbOrder.Quantity;
                }

                // Запрет «оплаты» отменённого заказа.
                if (newStatus == OrderStatus.Paid && dbOrder.Status == OrderStatus.Cancelled)
                {
                    MessageService.Error("Нельзя оплатить отменённый заказ.");
                    return;
                }

                dbOrder.Status = newStatus;
                db.SaveChanges();

                MessageService.Info(newStatus == OrderStatus.Paid
                    ? "Заказ отмечен как оплаченный."
                    : "Заказ отменён.");
                ReloadOrders();
                ReloadBooks();
                UpdateChart();
            }
            catch (Exception ex)
            {
                MessageService.Error($"Не удалось изменить статус заказа:\n{ex.Message}");
            }
        }

        private void DeleteOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersGrid.SelectedItem is not Order order)
            {
                MessageService.Warning("Выберите заказ для удаления.");
                return;
            }

            if (!MessageService.Confirm(
                    $"Удалить заказ №{order.Id} на сумму {order.TotalPrice:F2} ₽?",
                    "Подтверждение удаления"))
                return;

            try
            {
                using LibShopContext db = LibShopContext.Create();
                Order? dbOrder = db.Orders.Find(order.Id);
                if (dbOrder != null)
                {
                    // Возврат книг на склад, если заказ не отменён.
                    if (dbOrder.Status != OrderStatus.Cancelled &&
                        db.Books.Find(dbOrder.BookId) is { } book)
                    {
                        book.Quantity += dbOrder.Quantity;
                    }

                    db.Orders.Remove(dbOrder);
                    db.SaveChanges();
                }

                MessageService.Info("Заказ удалён.");
                ReloadOrders();
                ReloadBooks();
                UpdateChart();
            }
            catch (Exception ex)
            {
                MessageService.Error($"Не удалось удалить заказ:\n{ex.Message}");
            }
        }

        // ==================== Диаграмма ====================

        private void ChartMode_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && ChartItemsControl != null)
                UpdateChart();
        }

        private void RefreshChart_Click(object sender, RoutedEventArgs e) =>
            UpdateChart();

        /// <summary>Построение гистограммы по выбранному режиму.</summary>
        private void UpdateChart()
        {
            try
            {
                using LibShopContext db = LibShopContext.Create();
                var paidOrders = db.Orders.AsNoTracking()
                    .Include(o => o.Book)
                    .Where(o => o.Status == OrderStatus.Paid)
                    .ToList();

                var allOrders = db.Orders.AsNoTracking().ToList();

                List<ChartBar> bars;
                string mode = ChartModeComboBox.SelectedItem as string ?? ChartSalesByMonth;

                if (mode == ChartOrdersByStatus)
                {
                    var groups = new[]
                    {
                        new { Label = "Ожидает", Count = allOrders.Count(o => o.Status == OrderStatus.Pending) },
                        new { Label = "Оплачен", Count = allOrders.Count(o => o.Status == OrderStatus.Paid) },
                        new { Label = "Отменён", Count = allOrders.Count(o => o.Status == OrderStatus.Cancelled) },
                    };
                    bars = MakeBars(
                        groups.Select(g => (g.Label, (double)g.Count)),
                        v => $"{v:F0}");
                }
                else if (mode == ChartTopBooks)
                {
                    var top = paidOrders
                        .Where(o => o.Book != null)
                        .GroupBy(o => o.Book!.Title)
                        .Select(g => (Label: g.Key, Revenue: g.Sum(o => o.TotalPrice)))
                        .OrderByDescending(x => x.Revenue)
                        .Take(5);
                    bars = MakeBars(
                        top.Select(t => (t.Label, (double)t.Revenue)),
                        v => $"{v:F0} ₽");
                }
                else
                {
                    // Продажи по месяцам за последние 12 месяцев.
                    var months = Enumerable.Range(0, 12)
                        .Select(i => DateTime.Now.AddMonths(-i))
                        .OrderBy(d => d)
                        .Select(d => new { Key = (d.Year, d.Month), Label = d.ToString("MM.yyyy") })
                        .ToList();

                    var sales = paidOrders
                        .GroupBy(o => (o.OrderDate.Year, o.OrderDate.Month))
                        .ToDictionary(
                            g => g.Key,
                            g => g.Sum(o => o.TotalPrice));

                    bars = MakeBars(
                        months.Select(m => (
                            m.Label,
                            (double)(sales.TryGetValue(m.Key, out decimal sum) ? sum : 0))),
                        v => $"{v:F0} ₽");
                }

                ChartItemsControl.ItemsSource = bars;
            }
            catch (Exception ex)
            {
                MessageService.Error($"Не удалось построить диаграмму:\n{ex.Message}");
            }
        }

        /// <summary>Преобразует данные в столбцы диаграммы с масштабированием высоты.</summary>
        private static List<ChartBar> MakeBars(
            IEnumerable<(string Label, double Value)> data, Func<double, string> format)
        {
            var list = data.ToList();
            double max = list.Count > 0 ? list.Max(d => d.Value) : 0;

            const double maxBarHeight = 240;
            return list.Select(d => new ChartBar(
                d.Label,
                max > 0 ? Math.Max(4, d.Value / max * maxBarHeight) : 4,
                format(d.Value))).ToList();
        }

        // ==================== Отчёты ====================

        private void ExportXlsxButton_Click(object sender, RoutedEventArgs e) =>
            ExportOrdersReport(".xlsx");

        private void ExportDocxButton_Click(object sender, RoutedEventArgs e) =>
            ExportOrdersReport(".docx");

        private void ExportPdfButton_Click(object sender, RoutedEventArgs e) =>
            ExportOrdersReport(".pdf");

        /// <summary>Формирует отчёт по всем заказам и сохраняет в указанном формате.</summary>
        private void ExportOrdersReport(string extension)
        {
            try
            {
                using LibShopContext db = LibShopContext.Create();
                var orders = db.Orders.AsNoTracking()
                    .Include(o => o.User)
                    .Include(o => o.Book)
                    .OrderBy(o => o.OrderDate)
                    .ToList();

                if (orders.Count == 0)
                {
                    MessageService.Warning("В базе данных нет заказов для отчёта.");
                    return;
                }

                var headers = new List<string>
                {
                    "№", "Дата", "Пользователь", "Книга",
                    "Кол-во", "Цена, ₽", "Сумма, ₽", "Статус"
                };

                var rows = orders.Select(o => (IReadOnlyList<string>)new[]
                {
                    o.Id.ToString(),
                    o.OrderDate.ToString("dd.MM.yyyy HH:mm"),
                    o.User?.Login ?? "—",
                    o.Book?.Title ?? "—",
                    o.Quantity.ToString(),
                    o.UnitPrice.ToString("F2"),
                    o.TotalPrice.ToString("F2"),
                    o.StatusDescription
                }).ToList();

                const string title = "Отчёт по заказам магазина LibShop";

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Сохранить отчёт",
                    FileName = title,
                    DefaultExt = extension,
                    Filter = extension switch
                    {
                        ".docx" => "Документ Word (*.docx)|*.docx",
                        ".pdf" => "Документ PDF (*.pdf)|*.pdf",
                        _ => "Таблица Excel (*.xlsx)|*.xlsx"
                    }
                };

                if (dialog.ShowDialog() != true)
                    return;

                ExportService.Export(dialog.FileName, title, headers, rows);
                MessageService.Info($"Отчёт сохранён:\n{dialog.FileName}");
            }
            catch (Exception ex)
            {
                MessageService.Error($"Не удалось сформировать отчёт:\n{ex.Message}");
            }
        }
    }
}