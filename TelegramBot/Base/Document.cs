namespace TelegramBot.Base;

public abstract class Document
{
    public long Id { get; set; }
    public DateTime CreationDate { get; set; }
    public bool Deleted { get; set; }
    public DateTime?  DeletionDate { get; set; }

    protected Document()
    {
        Deleted = false;
        var now = DateTime.UtcNow;
        CreationDate = new DateTime(now.Ticks / 100000 * 100000, now.Kind);
    }
}