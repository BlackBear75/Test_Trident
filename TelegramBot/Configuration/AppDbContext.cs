using Microsoft.EntityFrameworkCore;
using TelegramBot.Entity.PhpScript;
using TelegramBot.Entity.User;

namespace TelegramBot.Configuration;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    
    public DbSet<PhpScript> PhpScripts { get; set; }
    

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 914220223,//TelegramId
                Username = "Bogdan_Porivay",
                Role = UserRole.Admin,
                CreationDate = new DateTime(2023, 2, 12),
                Deleted = false
            }
        );

        modelBuilder.Entity<PhpScript>()
            .Property(p => p.Id)
            .ValueGeneratedNever(); 

     
    }

   
}