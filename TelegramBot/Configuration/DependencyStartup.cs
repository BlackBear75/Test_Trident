using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using TelegramBot.API;
using TelegramBot.BusinessLogic;
using TelegramBot.Entity.PhpScript.Repository;
using TelegramBot.Entity.User.Repository;

namespace TelegramBot.Configuration
{
    public static class DependencyStartup
    {
        public static async Task RunAsync(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            var botService = host.Services.GetRequiredService<TelegramBotService>();
            await botService.StartAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    // Шлях до файлу конфігурації можна вказати статично
                    var configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                    config.AddJsonFile(configFilePath, optional: false, reloadOnChange: true);
            
                    // Або, якщо хочете, можна використовувати змінну середовища для шляху до файлу:
                    // var configFilePath = Environment.GetEnvironmentVariable("CONFIG_FILE_PATH") ?? "appsettings.json";

                    config.AddJsonFile(configFilePath, optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;

                    // Додання контексту БД
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

                    // Отримання токена Telegram-бота з конфігурації
                    var botToken = configuration["TelegramBotToken"];
                    if (string.IsNullOrEmpty(botToken))
                    {
                        throw new ArgumentException("Bot token is missing in configuration.");
                    }

                    // Додавання сервісів
                    services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
                    services.AddSingleton<TelegramBotService>();
                    services.AddSingleton<SftpService>();
                    services.AddSingleton<ScriptGeneratorService>();

                    // Реєстрація репозиторіїв
                    services.AddScoped(typeof(IUserRepository<>), typeof(UserRepository<>));
                    services.AddScoped(typeof(IPhpScriptRepository<>), typeof(PhpScriptRepository<>));
                })
                .UseConsoleLifetime();

    }
}