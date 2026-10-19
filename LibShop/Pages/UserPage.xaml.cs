using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LibShop.Data;
using LibShop.Models;
using LibShop.Services;
using Microsoft.EntityFrameworkCore;

namespace LibShop.Pages
{
    /// <summary>Страница пользователя: каталог книг (поиск/сортировка/фильтр) и свои заказы.</summary>
    public partial class UserPage : Page
    {
        private readonly MainWindow _window;
        private List<Book> _books = new();
        private List<Order> _orders = new();

        private const string SortByTitle = "По названию (А→Я)";
        private const string SortByTitleDesc = "По названию (Я→А)";
        private const string SortByPriceAsc = "По цене (сначала дешёвые)";
        private const string SortByPriceDesc = "По цене (сначала дорогие)";
        private const string SortByYearDesc = "По году издания (новые первыми)";
        private const string SortByYearAsc = "По году издания (старые первыми)";

        public UserPage(MainWindow window)
        {
            _window = window;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Настройка фильтров выполняется один раз при загрузке.
            if (GenreComboBox.Items.Count == 0)
            {
                GenreComboBox.Items.Add("Все жанры");
                foreach (string s in new[]
                {
                    SortByTitle, SortByTitleDesc, SortByPriceAsc,
                    SortByPriceDesc, SortByYearDesc, SortByYearAsc
                })
                    SortComboBox.Items.Add(s);
                foreach (string s in new[]
                {
                    "Все статусы", "Ожидает оплаты", "Оплачен", "Отменён"
                })
                    OrderStatusComboBox.Items.Add(s);

                GenreComboBox.SelectedIndex = 0;
                SortComboBox.SelectedIndex = 0;
                OrderStatusComboBox.SelectedIndex = 0;
            }

            ReloadBooks();
            ReloadOrders();
        }

        private void ReloadBooks()
        {
            try
            {
                using LibShopContext db = LibShopContext.Create();
                _books = db.Books.AsNoTracking().ToList();

                // Заполнение списка жанров из данных.
                string selectedGenre = GenreComboBox.SelectedItem as string ?? "Все жанры";
                var genres = _books.Select(b => b.Genre).Distinct().OrderBy(g => g).ToList();
                GenreComboBox.Items.Clear();
                GenreComboBox.Items.Add("Все жанры");
                foreach (string g in genres)
                    GenreComboBox.Items.Add(g);
                GenreComboBox.SelectedItem = genres.Contains(selectedGenre)
                    ? selectedGenre : "Все жанры";

                ApplyBookFilters();
            }
            catch (Exception ex)
            {
                MessageService.Error($"Не удалось загрузить каталог книг:\n{ex.Message}");
            }
        }

        /// <summary>Применение поиска, фильтра и сортировки к каталогу.</summary>
        private void ApplyBookFilters()
        {
            IEnumerable<Book> query = _books;

            // Поиск по названию или автору.
            string search = SearchTextBox.Text.Trim();
            if (search.Length > 0)
                query = query.Where(b =>
                    b.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    b.Author.Contains(search, StringComparison.OrdinalIgnoreCase));

            // Фильтр по жанру.
            if (GenreComboBox.SelectedItem is string genre && genre != "Все жанры")
                query = query.Where(b => b.Genre == genre);

            // Фильтр «только в наличии».
            if (InStockCheckBox.IsChecked == true)
                query = query.Where(b => b.Quantity > 0);

            // Сортировка.
            query = (SortComboBox.SelectedItem as string) switch
            {
                SortByTitleDesc => query.OrderByDescending(b => b.Title),
                SortByPriceAsc => query.OrderBy(b => b.Price),
                SortByPriceDesc => query.OrderByDescending(b => b.Price),
                SortByYearDesc => query.OrderByDescending(b => b.PublicationYear),
                SortByYearAsc => query.OrderBy(b => b.PublicationYear),
                _ => query.OrderBy(b => b.Title)
            };

            BooksGrid.ItemsSource = query.ToList();
        }

        private void Filters_Changed(object sender, RoutedEventArgs e)
        {
            if (IsLoaded && BooksGrid != null)
                ApplyBookFilters();
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            GenreComboBox.SelectedIndex = 0;
            SortComboBox.SelectedIndex = 0;
            InStockCheckBox.IsChecked = false;
        }

        // ==================== Покупка ====================

        private void DigitsOnly_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9]+$");

        private void BooksGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) =>
            BuySelectedBook();

        private void BuyButton_Click(object sender, RoutedEventArgs e) =>
            BuySelectedBook();

        private void BuySelectedBook()
        {
            if (BooksGrid.SelectedItem is not Book book)
            {
                MessageService.Warning("Выберите книгу в каталоге для покупки.");
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int quantity) ||
                quantity < 1 || quantity > 100)
            {
                MessageService.Warning("Количество должно быть целым числом от 1 до 100.");
                return;
            }

            if (book.Quantity < quantity)
            {
                MessageService.Warning(
                    $"Недостаточно экземпляров на складе. Доступно: {book.Quantity}.");
                return;
            }

            decimal total = book.Price * quantity;
            if (!MessageService.Confirm(
                    $"Оформить покупку?\n\nКнига: {book.Title}\nКоличество: {quantity}\n" +
                    $"Сумма: {total:F2} ₽",
                    "Подтверждение покупки"))
                return;

            try
            {
                using LibShopContext db = LibShopContext.Create();
                Book? dbBook = db.Books.Find(book.Id);
                User? dbUser = db.Users.Find(_window.CurrentUser!.Id);

                if (dbBook == null || dbUser == null || dbBook.Quantity < quantity)
                {
                    MessageService.Error("Не удалось оформить покупку: данных недостаточно.");
                    return;
                }

                db.Orders.Add(new Order
                {
                    UserId = dbUser.Id,
                    BookId = dbBook.Id,
                    Quantity = quantity,
                    UnitPrice = dbBook.Price,
                    TotalPrice = dbBook.Price * quantity,
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.Pending
                });

                dbBook.Quantity -= quantity; // Учёт остатков на складе.
                db.SaveChanges();

                MessageService.Info("Заказ оформлен и ожидает оплаты.");
                ReloadBooks();
                ReloadOrders();
            }
            catch (Exception ex)
            {
                MessageService.Error($"Не удалось оформить покупку:\n{ex.Message}");
            }
        }

        // ==================== Заказы ====================

        private void ReloadOrders()
        {
            try
            {
                using LibShopContext db = LibShopContext.Create();
                _orders = db.Orders.AsNoTracking()
                    .Include(o => o.Book)
                    .Where(o => o.UserId == _window.CurrentUser!.Id)
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
                    o.Book != null &&
                    o.Book.Title.Contains(search, StringComparison.OrdinalIgnoreCase));

            string? status = OrderStatusComboBox.SelectedItem as string;
            query = status switch
            {
                "Ожидает оплаты" => query.Where(o => o.Status == OrderStatus.Pending),
                "Оплачен" => query.Where(o => o.Status == OrderStatus.Paid),
                "Отменён" => query.Where(o => o.Status == OrderStatus.Cancelled),
                _ => query
            };

            OrdersGrid.ItemsSource = query.ToList();
        }

        private void Orders_FilterChanged(object sender, RoutedEventArgs e)
        {
            if (IsLoaded && OrdersGrid != null)
                ApplyOrderFilters();
        }

        private void RefreshOrders_Click(object sender, RoutedEventArgs e) =>
            ReloadOrders();

        private void CancelOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersGrid.SelectedItem is not Order order)
            {
                MessageService.Warning("Выберите заказ для отмены.");
                return;
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                MessageService.Warning("Этот заказ уже отменён.");
                return;
            }

            if (!MessageService.Confirm(
                    $"Отменить заказ №{order.Id}?\n\nКнига: {order.Book?.Title}\n" +
                    $"Сумма: {order.TotalPrice:F2} ₽\n\n" +
                    "Оплаченная сумма будет возвращена на счёт магазина.",
                    "Подтверждение отмены"))
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

                dbOrder.Status = OrderStatus.Cancelled;

                // Возврат экземпляров на склад.
                Book? book = db.Books.Find(dbOrder.BookId);
                if (book != null)
                    book.Quantity += dbOrder.Quantity;

                db.SaveChanges();

                MessageService.Info("Заказ отменён.");
                ReloadOrders();
                ReloadBooks();
            }
            catch (Exception ex)
            {
                MessageService.Error($"Не удалось отменить заказ:\n{ex.Message}");
            }
        }
    }
}