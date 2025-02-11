using TelegramBot.Base;
using TelegramBot.Base.Repository;

namespace TelegramBot.Entity.User.Repository;

public interface IUserRepository<TDocument> : IBaseRepository<TDocument> where TDocument : Document
{
   
}