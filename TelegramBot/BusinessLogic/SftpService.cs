using Renci.SshNet;
using System;
using System.IO;
using System.Threading.Tasks;
using TelegramBot.Entity.PhpScript;
using TelegramBot.Entity.PhpScript.Repository;

namespace TelegramBot.BusinessLogic
{
    public class SftpService
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
                  
                    script.SftpPassword = sftpPassword;
                    script.State = PhpScriptState.Upload;
                    
                    
                   await _phpscriptRepository.UpdateOneAsync(script);
                   
                   return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при завантаженні файлу: {ex.Message}");
                
                script.State = PhpScriptState.GenerationScript;
                
                await _phpscriptRepository.UpdateOneAsync(script);
                return false;
            }
        }

    }
}