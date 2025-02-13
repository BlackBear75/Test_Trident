using TelegramBot.Base;
using TelegramBot.Base.Repository;
using TelegramBot.Configuration;
using TelegramBot.Entity.User.Repository;

namespace TelegramBot.Entity.PhpScript.Repository;

public class PhpScriptRepository<TDocument> : BaseRepository<TDocument>, IPhpScriptRepository<TDocument> where TDocument : Document
{
    public PhpScriptRepository(AppDbContext  databaseConfiguration) : base(databaseConfiguration)
    {
    }
    
}
