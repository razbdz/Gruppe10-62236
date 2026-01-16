using Microsoft.EntityFrameworkCore;

namespace PackBot.Data;

public class PackBotDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<Account> Accounts { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Ligger ved app'en (samme mappe som .exe / bin output)
        optionsBuilder.UseSqlite("Data Source=packbot.sqlite");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().HasKey(o => o.Id);
        modelBuilder.Entity<Account>().HasKey(a => a.Username);
    }
}