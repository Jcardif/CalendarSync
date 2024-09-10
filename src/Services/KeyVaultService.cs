using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace CalendarSync.Services;

public class KeyVaultService(string vaultName)
{
    private readonly KeyVaultClient _vaultClient = new KeyVaultClient((authority, resource, scope) =>
        new AzureServiceTokenProvider().KeyVaultTokenCallback(authority, resource, scope));
    
    private readonly string _vaultUrl = $"https://{vaultName}.vault.azure.net/";
    
    
    public async Task<string> GetSecretAsync(string secretName)
    {
        var secret = await _vaultClient.GetSecretAsync(_vaultUrl, secretName);
        return secret.Value;
    }
}