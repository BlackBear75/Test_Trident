using TelegramBot.Base;
using TelegramBot.Base.Repository;

namespace TelegramBot.Entity.PhpScript.Repository;

public interface IPhpScriptRepository<TDocument> : IBaseRepository<TDocument> where TDocument : Document
{
    Task<TDocument> FindByTelegramIdWithoutUploadStateAsync(long telegramId);
    
    Task<IEnumerable<TDocument>> GetLastUploadsAsync(int count);
}