using Microsoft.Extensions.Logging;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.BusinessLogic;
using TelegramBot.Entity.User;
using TelegramBot.Entity.User.Repository;
using TelegramBot.Entity.PhpScript;
using TelegramBot.Entity.PhpScript.Repository;
using User = TelegramBot.Entity.User.User;

namespace TelegramBot.API;

public class TelegramBotService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceProvider _services;
    private readonly IUserRepository<User> _userRepository;
    private readonly ScriptGeneratorService _scriptGeneratorService;
    private readonly SftpService _sftpClientService;
    private readonly IPhpScriptRepository<PhpScript> _phpScriptRepository;

    public TelegramBotService(ITelegramBotClient botClient, IServiceProvider services,
        IUserRepository<User> userRepository, ScriptGeneratorService scriptGeneratorService,
        SftpService sftpClientService, IPhpScriptRepository<PhpScript> phpScriptRepository)
    {
        _botClient = botClient;
        _services = services;
        _userRepository = userRepository;
        _scriptGeneratorService = scriptGeneratorService;
        _sftpClientService = sftpClientService;
        _phpScriptRepository = phpScriptRepository;
    }

    public async Task StartAsync()
    {
        using var cts = new CancellationTokenSource();
        _botClient.StartReceiving(UpdateHandler, ErrorHandler, cancellationToken: cts.Token);
        Log.Information($"Bot started: {(await _botClient.GetMeAsync()).Username}");
        await Task.Delay(-1);
    }

    private async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message?.Text == null) return;

        var message = update.Message;
        var user = await _userRepository.FindByIdAsync(message.From.Id);

        if (user == null)
        {
            user = await CreateUserAsync(message.From);
        }

        if (user.Role == UserRole.Guest) return;
        
        var userScript = await _phpScriptRepository.FindByIdAsync(message.From.Id);

        if (message.Text.StartsWith("/admin"))
        {
            await HandleAdminCommands(message, user, cancellationToken);
            return;
        }

        await HandleUserCommands(message, user, userScript, cancellationToken);
    }

    private async Task<User> CreateUserAsync(Telegram.Bot.Types.User user)
    {

        var newUser = new User { Username = user.Username, Id = user.Id, Role = UserRole.Guest };
        await _userRepository.InsertOneAsync(newUser);
        Log.Information($"Created new user: {user.Username}");
        return newUser;
    }

    private async Task<PhpScript> CreateScriptAsync(long userId)
    {
        var script = new PhpScript { Id = userId, State = PhpScriptState.None };
        await _phpScriptRepository.InsertOneAsync(script);
        return script;
    }
private async Task HandleAdminCommands(Message message, User user, CancellationToken cancellationToken)
{
    var commandParts = message.Text.Split(' ');
    if (message.Text == "/admin")
    {   
        var adminCommands = @"
Команди адміністратора:
/admin users - Показати список користувачів.
/admin setrole {UserId} {Role} - Змінити роль.
/admin uploads - Останні завантаження.
/admin help - Довідка.";
        await _botClient.SendTextMessageAsync(message.Chat.Id, adminCommands, cancellationToken: cancellationToken);
        return;
    }
    switch (commandParts[1].ToLower()) 
    {
        case "users":
            var users = await _userRepository.GetAllAsync();
            var userList = string.Join("\n", users.Select(u => $"{u.Username} - {u.Id} - {u.Role}"));
            await _botClient.SendTextMessageAsync(message.Chat.Id, $"Список користувачів:\n{userList}", cancellationToken: cancellationToken);
            break;

        case "setrole":
            if (commandParts.Length < 3)
            {
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Формат: /admin setrole {UserId} {Role}", cancellationToken: cancellationToken);
                return;
            }
            
            if (!long.TryParse(commandParts[2], out var targetUserId) || !Enum.TryParse(commandParts[3], true, out UserRole newRole))
            {
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Неправильний формат! Використовуйте: /admin setrole {UserId} {Role}", cancellationToken: cancellationToken);
                return;
            }

            var targetUser = await _userRepository.FindByIdAsync(targetUserId);
            if (targetUser != null)
            {
                targetUser.Role = newRole;
                await _userRepository.UpdateOneAsync(targetUser);
                await _botClient.SendTextMessageAsync(message.Chat.Id, $"Роль користувача {targetUser.Username} змінена на {newRole}.", cancellationToken: cancellationToken);
            }
            else
            {
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Користувач не знайдений.", cancellationToken: cancellationToken);
            }
            break;

        case "uploads":
            var uploads = await _phpScriptRepository.GetLastUploadsAsync(10);
            var uploadList = string.Join("\n", uploads.Select(u => $"{u.Id} - {u.AppName} - {u.SftpHost}"));
            await _botClient.SendTextMessageAsync(message.Chat.Id, $"Останні 10 завантажень:\n{uploadList}", cancellationToken: cancellationToken);
            break;

        default:
            await _botClient.SendTextMessageAsync(message.Chat.Id, "Невідома команда адміністратора.", cancellationToken: cancellationToken);
            break;
    }
}

    private async Task HandleUserCommands(Message message, User user, PhpScript userScript, CancellationToken cancellationToken)
    {
        switch (message.Text.ToLower())
        {
            case "/start":
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Привіт! Уведіть /generate для створення скрипта.", cancellationToken: cancellationToken);
                break;

            case "/generate":
                if (user.Role < UserRole.User)
                {
                    await _botClient.SendTextMessageAsync(message.Chat.Id, "У вас нема прав для генерації скрипта.", cancellationToken: cancellationToken);
                    return;
                }
                userScript.State = PhpScriptState.WaitingForAppName;
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Уведіть назву додатку (AppName):", cancellationToken: cancellationToken);
                break;

            case "/upload":
                if (user.Role < UserRole.User || userScript.State < PhpScriptState.GenerationScript)
                {
                    await _botClient.SendTextMessageAsync(message.Chat.Id, "Недостаньо даних, згенеруйте скрипт /generate", cancellationToken: cancellationToken);
                    return;
                }
                userScript.State = PhpScriptState.WaitingForSftpHost;
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Уведіть данні для підключення к SFTP:\nХост:", cancellationToken: cancellationToken);
                break;

            default:
                await HandleDynamicInput(message, userScript, cancellationToken);
                break;
        }
    }

    private async Task HandleDynamicInput(Message message, PhpScript userScript, CancellationToken cancellationToken)
    {
        switch (userScript.State)
        {
            case PhpScriptState.WaitingForAppName:
                userScript.AppName = message.Text;
                userScript.State = PhpScriptState.WaitingForAppBundle;
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Уведіть AppBundle:", cancellationToken: cancellationToken);
                break;
            case PhpScriptState.WaitingForAppBundle:
                var phpScript = await _scriptGeneratorService.GeneratePhpScript(message.Text, userScript.Id);
                await _botClient.SendTextMessageAsync(message.Chat.Id, $"Генерація завершена!\nSecret: {phpScript.Secret}\nSecretParams: {phpScript.SecretKeyParam}\nЩоб відправити ваш скрипт уведіть /upload", cancellationToken: cancellationToken);
                break;
            case PhpScriptState.WaitingForSftpHost:
                userScript.SftpHost = message.Text;
                userScript.State = PhpScriptState.WaitingForSftpLogin;
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Уведіть логін для SFTP:", cancellationToken: cancellationToken);
                break;
            case PhpScriptState.WaitingForSftpLogin:
                userScript.SftpLogin = message.Text;
                userScript.State = PhpScriptState.WaitingForSftpPassword;
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Уведіть пароль для SFTP:", cancellationToken: cancellationToken);
                break;
            case PhpScriptState.WaitingForSftpPassword:
                var uploadResult = await _sftpClientService.UploadFileAsync(userScript, message.Text, userScript.ScriptContent, "/remote/path/script.php");
                await _botClient.SendTextMessageAsync(message.Chat.Id, uploadResult ? "Файл успешно завантажений!" : "Помилка завантаження.", cancellationToken: cancellationToken);
                break;
            default:
                await _botClient.SendTextMessageAsync(message.Chat.Id, "Невірна команда аба незавершенний процесс.", cancellationToken: cancellationToken);
                break;
        }
        await _phpScriptRepository.UpdateOneAsync(userScript);
    }

    private Task ErrorHandler(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Log.Error(exception, exception.Message + "\n" + exception.StackTrace);
        return Task.CompletedTask;
    }
}
