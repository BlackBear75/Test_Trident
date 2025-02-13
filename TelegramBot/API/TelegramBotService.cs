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

    public TelegramBotService(ITelegramBotClient botClient, IServiceProvider services, IUserRepository<User> userRepository, ScriptGeneratorService scriptGeneratorService, SftpService sftpClientService, IPhpScriptRepository<PhpScript> phpScriptRepository)
    {
        _userRepository = userRepository;
        _scriptGeneratorService = scriptGeneratorService;
        _botClient = botClient;
        _services = services;
        _sftpClientService = sftpClientService;
        _phpScriptRepository = phpScriptRepository;
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

        var user = await _userRepository.FindByTelegramIdAsync(message.From.Id);
        
        var userScript = await _phpScriptRepository.FindByTelegramIdAsync(message.From.Id);

       

        switch (message.Text.ToLower())
        {
            case "/start":
                if (user == null )
                {
                    await  _userRepository.InsertOneAsync(new User() {Username = message.From.Username,TelegramId = message.From.Id ,Role = UserRole.Guest});
                    await _phpScriptRepository.InsertOneAsync(new PhpScript() { TelegramId = message.From.Id ,State = PhpScriptState.None});
                    return;
                }
                await botClient.SendTextMessageAsync(message.Chat.Id, "Привет! Введите /generate для создания скрипта.", cancellationToken: cancellationToken);
                break;

            case "/generate":
                if (user.Role >= UserRole.User)
                {
                   
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Введите название приложения (AppName):", cancellationToken: cancellationToken);
                    userScript.State = PhpScriptState.WaitingForAppName;
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "У вас нет прав для генерации скрипта.", cancellationToken: cancellationToken);
                }
                break;

            case "/upload":
                if (user.Role >= UserRole.User)
                {
                    if (userScript.State < PhpScriptState.WaitingForSftpHost)
                    {
                        
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Недостаньо даних згенеруйте скрипт /generate", cancellationToken: cancellationToken);
                        return;
                    }
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Введите данные для подключения к SFTP:\nХост:", cancellationToken: cancellationToken);
                    userScript.State = PhpScriptState.WaitingForSftpHost;
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "У вас нет прав для загрузки скрипта.", cancellationToken: cancellationToken);
                }
                break;
            case "/admin":
                if (user.Role == UserRole.Admin)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Добро пожаловать в панель администратора.", cancellationToken: cancellationToken);

                    // Наприклад, вивести список усіх користувачів
                    var users = await _userRepository.GetAllAsync();
                    var userList = string.Join("\n", users.Select(u => $"{u.Username} - {u.TelegramId}"));
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Список користувачів:\n{userList}", cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "У вас нет прав для доступа к админ-панели.", cancellationToken: cancellationToken);
                }
                break;
            default:
                if (userScript.State == PhpScriptState.WaitingForAppName)
                {
                    userScript.State = PhpScriptState.WaitingForAppBundle;
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Введите AppBundle:", cancellationToken: cancellationToken);


                    if (userScript == null)
                    {
                        PhpScript phpScript = new PhpScript()
                        {
                            TelegramId = user.TelegramId,
                            AppName = message.Text,
                        };
                     await   _phpScriptRepository.InsertOneAsync(phpScript);
                    }
                    else
                    {
                        userScript.AppName = message.Text;
                     await  _phpScriptRepository.UpdateOneAsync(userScript);
                    }

                }
                else if (userScript.State == PhpScriptState.WaitingForAppBundle)
                {
                    
                    var phpScript = await _scriptGeneratorService.GeneratePhpScript(message.Text, user.TelegramId);
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Генерация завершена!\nSecret: {phpScript.Secret}\nSecretParams: {phpScript.SecretKeyParam}/n щоб відправити ваш скрипт уведіть /upload", cancellationToken: cancellationToken);
                }
                else if (userScript.State == PhpScriptState.WaitingForSftpHost)
                {
                    userScript.SftpHost = message.Text;
                    userScript.State = PhpScriptState.WaitingForSftpLogin;
                    await _phpScriptRepository.UpdateOneAsync(userScript); 
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Введите логин для SFTP:", cancellationToken: cancellationToken);
                }
                else if (userScript.State == PhpScriptState.WaitingForSftpLogin)
                {
                    userScript.SftpLogin = message.Text;
                    userScript.State = PhpScriptState.WaitingForSftpPassword;
                    await _phpScriptRepository.UpdateOneAsync(userScript); 
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Введите пароль для SFTP:", cancellationToken: cancellationToken);
                }
                else if (userScript.State == PhpScriptState.WaitingForSftpPassword)
                {

                    var phpScriptToUpload = await _phpScriptRepository.FindByTelegramIdAsync(user.TelegramId);
                    if (phpScriptToUpload != null)
                    {
                     bool result =   await _sftpClientService.UploadFileAsync(phpScriptToUpload, message.Text, phpScriptToUpload.ScriptContent, "/remote/path/script.php");

                     if (result)
                     {
                         await botClient.SendTextMessageAsync(message.Chat.Id, "Файл был успешно загружен на сервер.", cancellationToken: cancellationToken);
                     }
                     else
                     {
                         await botClient.SendTextMessageAsync(message.Chat.Id, "Помилка при завантаженні на сервер", cancellationToken: cancellationToken);
                     }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Не знайдено скрипт для завантаження.", cancellationToken: cancellationToken);
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Неизвестная команда.", cancellationToken: cancellationToken);
                }
                break;
        }
    }

    private Task ErrorHandler(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Error: {exception.Message}");
        return Task.CompletedTask;
    }
}
