using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.API;
using TelegramBot.BusinessLogic;
using TelegramBot.Entity.PhpScript.Repository;
using TelegramBot.Entity.ScriptUpload.Repository;
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
                    config.AddJsonFile(@"C:\Home\Test_Trident\Test_Trident\TelegramBot\appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

                    var botToken = configuration["TelegramBotToken"];
                    if (string.IsNullOrEmpty(botToken))
                    {
                        throw new ArgumentException("Bot token is missing in configuration.");
                    }
                    services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
                    services.AddSingleton<TelegramBotService>();
                    
                    services.AddSingleton<SftpService>();
                    
                    services.AddSingleton<ScriptGeneratorService>();
                    
                    services.AddScoped(typeof(IUserRepository<>), typeof(UserRepository<>));
                    
                    services.AddScoped(typeof(IScriptUploadRepository<>), typeof(ScriptUploadRepository<>));
                    services.AddScoped(typeof(IPhpScriptRepository<>), typeof(PhpScriptRepository<>));
                    
                })
                .UseConsoleLifetime();
    }
}