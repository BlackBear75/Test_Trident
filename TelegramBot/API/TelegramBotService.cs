using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBot.API;

public class TelegramBotService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceProvider _services;

    public TelegramBotService(ITelegramBotClient botClient, IServiceProvider services)
    {
        _botClient = botClient;
        _services = services;
    }

    public async Task StartAsync()
    {
        using var cts = new CancellationTokenSource();
        _botClient.StartReceiving(UpdateHandler, ErrorHandler, cancellationToken: cts.Token);
        var me = await _botClient.GetMeAsync();
        Console.WriteLine($"Bot started: {me.Username}");
        await Task.Delay(-1);
    }

    private async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message) return;

        var message = update.Message;
        if (message?.Text == null) return;

        switch (message.Text.ToLower())
        {
            case "/start":
                await botClient.SendTextMessageAsync(message.Chat.Id, "Привет! Введите /generate для создания скрипта.", cancellationToken: cancellationToken);
                break;

            case "/generate":
                // Тут буде логіка генерації PHP скрипта
                await botClient.SendTextMessageAsync(message.Chat.Id, "Генерация скрипта начата...", cancellationToken: cancellationToken);
                break;

            case "/upload":
                // Тут буде логіка завантаження скрипта на SFTP
                await botClient.SendTextMessageAsync(message.Chat.Id, "Начинаем загрузку на сервер...", cancellationToken: cancellationToken);
                break;

            default:
                await botClient.SendTextMessageAsync(message.Chat.Id, "Неизвестная команда.", cancellationToken: cancellationToken);
                break;
        }
    }

    private Task ErrorHandler(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Error: {exception.Message}");
        return Task.CompletedTask;
    }
}