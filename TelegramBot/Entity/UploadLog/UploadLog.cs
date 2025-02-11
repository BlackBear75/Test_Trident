using TelegramBot.Base;

namespace TelegramBot.Entity.UploadLog;

public class UploadLog : Document
{
    public string AppName { get; set; }
    public string FilePath { get; set; }
    public DateTime UploadDate { get; set; }
    public Guid UserId { get; set; }
}