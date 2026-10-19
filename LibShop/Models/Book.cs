namespace LibShop.Models;

/// <summary>Книга, представленная в магазине.</summary>
public class Book
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public string Genre { get; set; } = string.Empty;

    public string? Publisher { get; set; }

    /// <summary>Год издания (1000–2100).</summary>
    public int PublicationYear { get; set; }

    /// <summary>Цена за экземпляр, ₽.</summary>
    public decimal Price { get; set; }

    /// <summary>Количество экземпляров в наличии.</summary>
    public int Quantity { get; set; }

    public string? Description { get; set; }

    /// <summary>Заказы, включающие книгу (каскадное удаление).</summary>
    public List<Order> Orders { get; set; } = new();
}
