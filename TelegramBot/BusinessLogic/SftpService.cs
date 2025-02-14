using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Serilog;
using TelegramBot.Entity.PhpScript;
using TelegramBot.Entity.PhpScript.Repository;

namespace TelegramBot.BusinessLogic
{
    public interface ISftpService
    {
        Task<bool> UploadFileAsync(PhpScript script, string sftpPassword, string fileContent, string remotePath);
        
    }
    public class SftpService : ISftpService
    {
        private readonly IPhpScriptRepository<PhpScript> _phpscriptRepository;

        public SftpService(IPhpScriptRepository<PhpScript> phpscriptRepository)
        {
            _phpscriptRepository = phpscriptRepository;
        }
    
        public async Task<bool> UploadFileAsync(PhpScript script, string sftpPassword, string fileContent, string remotePath)
        {
            try
            {
                Log.Information($"Початок завантаження файлу на сервер для користувача {script.Id}");

                using (var sftpClient = new SftpClient(script.SftpHost, script.SftpLogin, sftpPassword))
                {
                    sftpClient.Connect();

                    if (!sftpClient.IsConnected)
                    {
                        Log.Error($"Не вдалося підключитись до сервера {script.SftpHost}.");
                        return false;
                    }

                    var fileBytes = System.Text.Encoding.UTF8.GetBytes(fileContent);

                    using (var memoryStream = new MemoryStream(fileBytes))
                    {
                        await Task.Run(() => sftpClient.UploadFile(memoryStream, remotePath, true));
                    }

                    sftpClient.Disconnect();
                    
                    script.SftpPassword = sftpPassword;
                    script.State = PhpScriptState.Upload;
                    
                    await _phpscriptRepository.UpdateOneAsync(script);
                    Log.Information("Файл успішно відправлений на сервер.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Помилка при завантаженні файлу для користувача {script.Id}. Стан: {script.State}");

                script.State = PhpScriptState.GenerationScript;
                await _phpscriptRepository.UpdateOneAsync(script);
                return false;
            }
        }
    }
}
