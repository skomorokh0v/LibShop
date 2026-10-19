using LibShop.Models;
using Microsoft.EntityFrameworkCore;

namespace LibShop.Data;

/// <summary>Контекст базы данных книжного магазина (Microsoft SQL Server).</summary>
public class LibShopContext : DbContext
{
    /// <summary>Строка подключения к серверу HOME-PC.</summary>
    public const string ConnectionString =
        "Server=HOME-PC;Database=LibShop;Trusted_Connection=True;TrustServerCertificate=True;";

    public DbSet<User> Users => Set<User>();

    public DbSet<Book> Books => Set<Book>();

    public DbSet<Order> Orders => Set<Order>();

    /// <summary>Создаёт контекст с подключением к серверу HOME-PC.</summary>
    public static LibShopContext Create() =>
        new(new DbContextOptionsBuilder<LibShopContext>()
            .UseSqlServer(ConnectionString)
            .Options);

    public LibShopContext(DbContextOptions<LibShopContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Login).IsUnique();
            entity.Property(u => u.Login).HasMaxLength(20).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(88).IsRequired();
            entity.Property(u => u.PasswordSalt).HasMaxLength(44).IsRequired();
            entity.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(100);
            entity.Property(u => u.Phone).HasMaxLength(20);
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(10);
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.Property(b => b.Title).HasMaxLength(200).IsRequired();
            entity.Property(b => b.Author).HasMaxLength(100).IsRequired();
            entity.Property(b => b.Genre).HasMaxLength(50).IsRequired();
            entity.Property(b => b.Publisher).HasMaxLength(100);
            entity.Property(b => b.Price).HasPrecision(12, 2);
            entity.Property(b => b.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(o => o.UnitPrice).HasPrecision(12, 2);
            entity.Property(o => o.TotalPrice).HasPrecision(12, 2);
            entity.Property(o => o.Status).HasConversion<string>().HasMaxLength(10);

            // Каскадное удаление: при удалении пользователя или книги
            // связанные заказы удаляются автоматически.
            entity.HasOne(o => o.User)
                  .WithMany(u => u.Orders)
                  .HasForeignKey(o => o.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(o => o.Book)
                  .WithMany(b => b.Orders)
                  .HasForeignKey(o => o.BookId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
