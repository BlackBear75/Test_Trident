using Microsoft.Extensions.Logging;
using Serilog;
using TelegramBot.Entity.PhpScript;
using TelegramBot.Entity.PhpScript.Repository;

namespace TelegramBot.BusinessLogic
{
    public  class ScriptGeneratorService
    {
        private readonly IPhpScriptRepository<PhpScript> _phpScriptRepository;

        public  ScriptGeneratorService(IPhpScriptRepository<PhpScript> phpScriptRepository)
        {
            _phpScriptRepository = phpScriptRepository;
        }

        public async Task<PhpScript> GeneratePhpScript(string appBundle, long userId)
        {
            var secret = GenerateSecret();
            var secretKeyParam = GenerateSecretKeyParam();
            var userscript = await _phpScriptRepository.FindByIdAsync(userId);

            string phpScriptContent = $@"
<?php
$appName = '{userscript.AppName}';
$appBundle = '{appBundle}';
$secretKey = '{secret}';
if($secretKey == $_GET['{secretKeyParam}']){{
    echo 'Привіт я додаток {userscript.AppName} моє посилання на гугл плей https://play.google.com/store/apps/details?id={appBundle}';
}}
";

           
                userscript.AppBundle = appBundle;
                userscript.Secret = secret;
                userscript.SecretKeyParam = secretKeyParam;
                userscript.ScriptContent = phpScriptContent;
                userscript.State = PhpScriptState.GenerationScript;
                userscript.SftpHost = "";
                userscript.SftpLogin = "";

                
                await _phpScriptRepository.UpdateOneAsync(userscript);
               Log.Information("Генерація скрипта успішна");
            
                
                
            return userscript;
        }

        private  string GenerateSecret()
        {
            return Guid.NewGuid().ToString(); 
        }

        private  string GenerateSecretKeyParam()
        {
            return Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16); 
        }
    }
}