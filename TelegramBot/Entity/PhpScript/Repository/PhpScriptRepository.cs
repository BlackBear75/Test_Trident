using Microsoft.EntityFrameworkCore;
using TelegramBot.Base;
using TelegramBot.Base.Repository;
using TelegramBot.Configuration;
using TelegramBot.Entity.User.Repository;

namespace TelegramBot.Entity.PhpScript.Repository;

public class PhpScriptRepository<TDocument> : BaseRepository<TDocument>, IPhpScriptRepository<TDocument>
    where TDocument : Document
{
    public PhpScriptRepository(AppDbContext databaseConfiguration) : base(databaseConfiguration)
    {
    }

    public async Task<TDocument> FindByTelegramIdWithoutUploadStateAsync(long telegramId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(d => d.Id == telegramId && !d.Deleted &&
                                      EF.Property<int>(d, "State") != (int)PhpScriptState.Upload);
    }

    public async Task<IEnumerable<TDocument>> GetLastUploadsAsync(int count)
    {
        return await _dbSet
            .Where(script =>  EF.Property<int>(script, "State") == (int)PhpScriptState.Upload) 
            .OrderByDescending(script => script.CreationDate) 
            .Take(count) 
            .ToListAsync();
    }
}
