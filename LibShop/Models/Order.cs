namespace LibShop.Models;

/// <summary>Статус заказа (платежа).</summary>
public enum OrderStatus
{
    /// <summary>Заказ ожидает оплаты.</summary>
    Pending,

    /// <summary>Заказ оплачен.</summary>
    Paid,

    /// <summary>Заказ отменён.</summary>
    Cancelled
}

/// <summary>Заказ (платёж) пользователя за книгу.</summary>
public class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public int BookId { get; set; }
    public Book? Book { get; set; }

    /// <summary>Количество экземпляров (минимум 1).</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Цена за экземпляр на момент заказа, ₽.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Итоговая сумма заказа, ₽.</summary>
    public decimal TotalPrice { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;

    public OrderStatus Status { get; set; } = OrderStatus.Paid;

    /// <summary>Человекочитаемое описание статуса (для отображения в интерфейсе).</summary>
    public string StatusDescription => Status switch
    {
        OrderStatus.Pending => "Ожидает оплаты",
        OrderStatus.Paid => "Оплачен",
        OrderStatus.Cancelled => "Отменён",
        _ => Status.ToString()
    };
}
