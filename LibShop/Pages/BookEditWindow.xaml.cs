using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using LibShop.Models;

namespace LibShop.Pages
{
    /// <summary>Окно добавления/редактирования книги с проверкой данных.</summary>
    public partial class BookEditWindow : Window
    {
        private static readonly Regex PriceRegex = new(@"^[0-9]+([.,][0-9]{1,2})?$");

        /// <summary>Редактируемая книга (null — новая). После «Сохранить» содержит введённые данные.</summary>
        public Book? Result { get; private set; }

        public BookEditWindow(Book? book = null)
        {
            InitializeComponent();

            if (book != null)
            {
                TitleLabel.Text = "Редактирование книги";
                Title = "Редактирование книги";
                TitleTextBox.Text = book.Title;
                AuthorTextBox.Text = book.Author;
                GenreTextBox.Text = book.Genre;
                PublisherTextBox.Text = book.Publisher ?? string.Empty;
                YearTextBox.Text = book.PublicationYear.ToString();
                PriceTextBox.Text = book.Price.ToString("0.##", CultureInfo.InvariantCulture);
                QuantityTextBox.Text = book.Quantity.ToString();
                DescriptionTextBox.Text = book.Description ?? string.Empty;
            }
        }

        private void DigitsOnly_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");

        private void Price_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            // Цифры, один десятичный разделитель (запятая или точка) — Copy/Paste пропускается.
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9.,]$");

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = string.Empty;

            string title = TitleTextBox.Text.Trim();
            string author = AuthorTextBox.Text.Trim();
            string genre = GenreTextBox.Text.Trim();
            string publisher = PublisherTextBox.Text.Trim();

            if (title.Length == 0)
            {
                ErrorText.Text = "Укажите название книги.";
                return;
            }
            if (author.Length == 0)
            {
                ErrorText.Text = "Укажите автора.";
                return;
            }
            if (genre.Length == 0)
            {
                ErrorText.Text = "Укажите жанр.";
                return;
            }
            if (!int.TryParse(YearTextBox.Text, out int year) ||
                year < 1000 || year > 2100)
            {
                ErrorText.Text = "Год издания — целое число от 1000 до 2100.";
                return;
            }

            // Цена: принимаем точку и запятую, не более 2 знаков после запятой.
            string priceText = PriceTextBox.Text.Replace(',', '.');
            if (!PriceRegex.IsMatch(PriceTextBox.Text) ||
                !decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.InvariantCulture,
                    out decimal price) ||
                price < 0 || price > 1_000_000)
            {
                ErrorText.Text = "Цена — число от 0 до 1 000 000 (не более 2 знаков после запятой).";
                return;
            }
            if (!int.TryParse(QuantityTextBox.Text, out int quantity) ||
                quantity < 0 || quantity > 100_000)
            {
                ErrorText.Text = "Количество — целое число от 0 до 100 000.";
                return;
            }

            Result = new Book
            {
                Title = title,
                Author = author,
                Genre = genre,
                Publisher = publisher.Length == 0 ? null : publisher,
                PublicationYear = year,
                Price = price,
                Quantity = quantity,
                Description = DescriptionTextBox.Text.Trim().Length == 0
                    ? null
                    : DescriptionTextBox.Text.Trim()
            };

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) =>
            DialogResult = false;
    }
}