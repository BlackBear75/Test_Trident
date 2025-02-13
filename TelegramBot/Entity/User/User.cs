using TelegramBot.Base;

namespace TelegramBot.Entity.User;

public class User : Document
{
    public long TelegramId { get; set; }
    public string Username { get; set; }
    public UserRole Role { get; set; } 
}

public enum UserRole
{
    Guest = 0,
    User = 1,
    Admin = 2
}

