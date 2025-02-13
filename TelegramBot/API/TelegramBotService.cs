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

        var user = await _userRepository.FindByIdAsync(message.From.Id);
        
        var userScript = await _phpScriptRepository.FindByTelegramIdWithoutUploadStateAsync(message.From.Id);

       

        switch (message.Text.ToLower())
        {
            case "/start":
                if (user == null )
                {
                    await  _userRepository.InsertOneAsync(new User() {Username = message.From.Username,Id = message.From.Id ,Role = UserRole.Guest});
                    return;
                }
                await botClient.SendTextMessageAsync(message.Chat.Id, "Привет! Введите /generate для создания скрипта.", cancellationToken: cancellationToken);
                break;

            case "/generate":
                if (user.Role >= UserRole.User)
                {
                   
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Введите название приложения (AppName):", cancellationToken: cancellationToken);
                    if (userScript == null)
                    { 
                        await _phpScriptRepository.InsertOneAsync(new PhpScript() {Id = message.From.Id,State = PhpScriptState.None});
                       var newScript = await _phpScriptRepository.FindByIdAsync(message.From.Id);
                       userScript = newScript;
                    }

                 
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
                    if (userScript.State < PhpScriptState.GenerationScript)
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
            case "/admin help":
                if (user.Role == UserRole.Admin)
                {
                    var adminCommands = @"
Команди адміністратора:
- /admin users - Показати список всіх користувачів.
- /admin setrole {UserId} {Role} - Змінити роль користувача.
- /admin uploads - Показати останні завантаження.
- /admin help - Показати список команд адміністратора.";
                    await botClient.SendTextMessageAsync(message.Chat.Id, adminCommands, cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "У вас немає прав для перегляду команд адміністратора.", cancellationToken: cancellationToken);
                }
                break;

           case "/admin users":
              if (user.Role == UserRole.Admin)
              {
                  var users = await _userRepository.GetAllAsync();
                  var userList = string.Join("\n", users.Select(u => $"{u.Username} - {u.Id} - {u.Role}"));
                  await botClient.SendTextMessageAsync(message.Chat.Id, $"Список користувачів:\n{userList}", cancellationToken: cancellationToken);
              }
              else
              {
                  await botClient.SendTextMessageAsync(message.Chat.Id, "У вас немає прав для перегляду користувачів.", cancellationToken: cancellationToken);
              }
              break;

            case "/admin setrole":
                if (user.Role == UserRole.Admin)
                {
                    var parts = message.Text.Split(' ');

                    if (parts.Length < 3)
                    {
                        var rolesHelp = @"
Доступні ролі:
1. Guest - Гість, обмежений доступ.
2. User - Звичайний користувач, має доступ до основних функцій.
3. Admin - Адміністратор, має доступ до всіх функцій.

Формат: /admin setrole {UserId} {Role}
Приклад: /admin setrole 123456789 User";
                        await botClient.SendTextMessageAsync(message.Chat.Id, rolesHelp, cancellationToken: cancellationToken);
                        break;
                    }

                    if (!long.TryParse(parts[1], out var targetUserId) || !Enum.TryParse(parts[2], true, out UserRole newRole))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Формат: /admin setrole {UserId} {Role}", cancellationToken: cancellationToken);
                        break;
                    }

                    var targetUser = await _userRepository.FindByIdAsync(targetUserId);
                    if (targetUser != null)
                    {
                        targetUser.Role = newRole;
                        await _userRepository.UpdateOneAsync(targetUser);
                        await botClient.SendTextMessageAsync(message.Chat.Id, $"Роль користувача {targetUser.Username} змінена на {newRole}.", cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Користувач не знайдений.", cancellationToken: cancellationToken);
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "У вас немає прав для зміни ролей.", cancellationToken: cancellationToken);
                }
                break;

            case "/admin uploads": 
                
                if (user.Role == UserRole.Admin)
                {
                    var uploads = await _phpScriptRepository.GetLastUploadsAsync(10);
                    var uploadList = string.Join("\n", uploads.Select(u => $"{u.Id} - {u.AppName} - {u.SftpHost}"));
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Останні 10 завантажень:\n{uploadList}", cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "У вас немає прав для перегляду завантажень.", cancellationToken: cancellationToken);
                }
                break;

            default:
                if (message.Text.StartsWith("/"))
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Невірна команда. Завершіть попередній процес або введіть коректні дані.", cancellationToken: cancellationToken);
                    return;
                }
                if (userScript.State == PhpScriptState.WaitingForAppName)
                {
                    userScript.State = PhpScriptState.WaitingForAppBundle;
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Введите AppBundle:", cancellationToken: cancellationToken);
                    
                
                    userScript.AppName = message.Text;
                    await  _phpScriptRepository.UpdateOneAsync(userScript);
                }
                
                else if (userScript.State == PhpScriptState.WaitingForAppBundle)
                {
                    var phpScript = await _scriptGeneratorService.GeneratePhpScript(message.Text, user.Id);
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Генерация завершена!\nSecret: {phpScript.Secret}\nSecretParams: {phpScript.SecretKeyParam}\nщоб відправити ваш скрипт уведіть /upload", cancellationToken: cancellationToken);
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

                    var phpScriptToUpload = await _phpScriptRepository.FindByIdAsync(user.Id);
                    if (phpScriptToUpload != null)
                    {
                     bool result =   await _sftpClientService.UploadFileAsync(phpScriptToUpload, message.Text, phpScriptToUpload.ScriptContent, "/remote/path/script.php");

                     if (result)
                     {
                         await botClient.SendTextMessageAsync(message.Chat.Id, "Файл був успішно завантажений на сервер.", cancellationToken: cancellationToken);
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
