using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Telegram.Bot;
using TelegramBot.API;
using TelegramBot.BusinessLogic;
using TelegramBot.Entity.PhpScript.Repository;
using TelegramBot.Entity.User.Repository;
using ILogger = Serilog.ILogger;

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
                .ConfigureServices((context, services) =>
                {
                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Information() 
                        .WriteTo.Console()  
                        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)  
                        .CreateLogger();

                    services.AddSingleton<ILogger>(Log.Logger); 

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
                    services.AddSingleton<ISftpService,SftpService>();
                    services.AddSingleton<IScriptGeneratorService,ScriptGeneratorService>();
                    
                    services.AddSingleton<ITelegramBotClientWrapper, TelegramBotClientWrapper>();

                    services.AddScoped(typeof(IUserRepository<>), typeof(UserRepository<>));
                    services.AddScoped(typeof(IPhpScriptRepository<>), typeof(PhpScriptRepository<>));
                })
                .UseConsoleLifetime();
    }
}
