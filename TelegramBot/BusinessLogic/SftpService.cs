using Renci.SshNet;
using System;
using System.IO;
using System.Threading.Tasks;
using TelegramBot.Entity.PhpScript;
using TelegramBot.Entity.ScriptUpload;
using TelegramBot.Entity.ScriptUpload.Repository;

namespace TelegramBot.BusinessLogic
{
    public class SftpService
    {
        private readonly IScriptUploadRepository<ScriptUpload> _scriptUploadRepository;

        public SftpService(IScriptUploadRepository<ScriptUpload> scriptUploadRepository)
        {
            _scriptUploadRepository = scriptUploadRepository;
        }
    
        public async Task<bool> UploadFileAsync(PhpScript script, string sftpPassword, string fileContent, string remotePath)
        {
            try
            {
                using (var sftpClient = new SftpClient(script.SftpHost, script.SftpLogin, sftpPassword))
                {
                    sftpClient.Connect();

                    if (!sftpClient.IsConnected)
                    {
                        Console.WriteLine("Не вдалося підключитись до сервера.");
                        return false;
                    }

                    var fileBytes = System.Text.Encoding.UTF8.GetBytes(fileContent);

                    using (var memoryStream = new MemoryStream(fileBytes))
                    {
                        await Task.Run(() => sftpClient.UploadFile(memoryStream, remotePath, true));
                    }

                    sftpClient.Disconnect();
                    ScriptUpload scriptUpload = new ScriptUpload()
                    {
                        TelegramId = script.TelegramId,
                        SftpHost = script.SftpHost,
                        Secret = script.Secret,
                        SecretKeyParam = script.SecretKeyParam,
                        AppName = script.AppName,
                        AppBundle = script.AppBundle,
                        SftpLogin = script.SftpLogin,
                        SftpPassword = sftpPassword,
                        Success = true
                    };
                   await _scriptUploadRepository.InsertOneAsync(scriptUpload);
                   
                   return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при завантаженні файлу: {ex.Message}");
                return false;
            }
        }

    }
}