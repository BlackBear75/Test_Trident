using TelegramBot.Base;

namespace TelegramBot.Entity.ScriptUpload
{
    public class ScriptUpload : Document
    {
        public long TelegramId { get; set; } 
        public string AppName { get; set; } 
        public string AppBundle { get; set; } 
        public string Secret { get; set; } 
        public string SecretKeyParam { get; set; } 
        public string SftpHost { get; set; } 
        
        public string SftpLogin{ get; set; } 
        
        public string SftpPassword{ get; set; } 
        
      //  public string SftpPath { get; set; } 
        public bool Success { get; set; } 
    }
}