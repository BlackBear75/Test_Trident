using Microsoft.EntityFrameworkCore;
using TelegramBot.Entity.UploadLog;
using TelegramBot.Entity.User;

namespace TelegramBot.Configuration;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<UploadLog> UploadLogs { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}