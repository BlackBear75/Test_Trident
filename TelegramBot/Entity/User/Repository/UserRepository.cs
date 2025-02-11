using TelegramBot.Base;
using TelegramBot.Base.Repository;
using TelegramBot.Configuration;

namespace TelegramBot.Entity.User.Repository;

public class UserRepository<TDocument> : BaseRepository<TDocument>, IUserRepository<TDocument> where TDocument : Document
{
    public UserRepository(AppDbContext  databaseConfiguration) : base(databaseConfiguration)
    {
    }
    
}