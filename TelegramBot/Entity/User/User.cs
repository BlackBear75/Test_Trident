using TelegramBot.Base;

namespace TelegramBot.Entity.User;

public class User : Document
{
    public long TelegramId { get; set; }
    public string Username { get; set; }
    public string Role { get; set; } // Guest, User, Admin
}