using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBot.API;

public interface ITelegramBotClientWrapper
{
    Task<Message> SendMessage(
        ChatId chatId,
        string text,
        int? messageThreadId = default,
        ParseMode parseMode = default,
        IEnumerable<MessageEntity>? entities = default,
        LinkPreviewOptions? linkPreviewOptions = default,
        bool disableNotification = default,
        bool protectContent = default,
        bool allowPaidBroadcast = default,
        string? messageEffectId = default,
        ReplyParameters? replyParameters = default,
        IReplyMarkup? replyMarkup = default,
        string? businessConnectionId = default,
        CancellationToken cancellationToken = default
    );
}

public class TelegramBotClientWrapper : ITelegramBotClientWrapper
{
    private readonly ITelegramBotClient _botClient;

    public TelegramBotClientWrapper(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public Task<Message> SendMessage(
        ChatId chatId,
        string text,
        int? messageThreadId = default,
        ParseMode parseMode = default,
        IEnumerable<MessageEntity>? entities = default,
        LinkPreviewOptions? linkPreviewOptions = default,
        bool disableNotification = default,
        bool protectContent = default,
        bool allowPaidBroadcast = default,
        string? messageEffectId = default,
        ReplyParameters? replyParameters = default,
        IReplyMarkup? replyMarkup = default,
        string? businessConnectionId = default,
        CancellationToken cancellationToken = default
    )
    {
        return _botClient.SendTextMessageAsync(
            chatId, text, messageThreadId, parseMode, entities, linkPreviewOptions,
            disableNotification, protectContent, allowPaidBroadcast, messageEffectId,
            replyParameters, replyMarkup, businessConnectionId, cancellationToken
        );
    }
}