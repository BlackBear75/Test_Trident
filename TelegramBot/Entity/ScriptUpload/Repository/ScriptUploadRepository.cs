using TelegramBot.Base;
using TelegramBot.Base.Repository;
using TelegramBot.Configuration;
using TelegramBot.Entity.User.Repository;

namespace TelegramBot.Entity.ScriptUpload.Repository;

public class ScriptUploadRepository<TDocument> : BaseRepository<TDocument>, IScriptUploadRepository<TDocument> where TDocument : Document
{
    public ScriptUploadRepository(AppDbContext  databaseConfiguration) : base(databaseConfiguration)
    {
    }
    
        
    
}
