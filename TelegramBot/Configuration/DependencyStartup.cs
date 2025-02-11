using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using TelegramBot.API;

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
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;

                    ConnectionString(services, configuration);
                    ConfigureTelegramBotService(services, configuration);

                })
                .UseConsoleLifetime();

        private static void ConnectionString (IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            
        }

        private static void ConfigureTelegramBotService(IServiceCollection services,IConfiguration configuration)
        {
            services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(configuration["TelegramBotToken"]));
            services.AddSingleton<TelegramBotService>();
        }
    }
}