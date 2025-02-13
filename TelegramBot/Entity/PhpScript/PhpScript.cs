using TelegramBot.Base;

namespace TelegramBot.Entity.PhpScript;

public class PhpScript:Document
{
    
    public string? AppName { get; set; }
    
    public string? AppBundle { get; set; }
    
    public string? Secret { get; set; }
    
    public string? SecretKeyParam { get; set; }
    
    public string? ScriptContent { get; set; }
    
    public string? SftpHost { get; set; } 
    
    public string? SftpPassword { get; set; } 
    public string? SftpLogin { get; set; } 
    
    public PhpScriptState State { get; set; }
    
}



public enum PhpScriptState
{
    None,            
    WaitingForAppName,
    WaitingForAppBundle, 
    GenerationScript, 
    WaitingForSftpHost,
    WaitingForSftpLogin, 
    WaitingForSftpPassword ,
    Upload
}