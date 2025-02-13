using TelegramBot.Base;
using TelegramBot.Base.Repository;

namespace TelegramBot.Entity.ScriptUpload.Repository;

public interface IScriptUploadRepository<TDocument> : IBaseRepository<TDocument> where TDocument : Document
{
    
}