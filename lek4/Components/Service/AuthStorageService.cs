using Microsoft.Maui.Storage;
using System.Threading.Tasks;

public class AuthStorageService
{
    private const string TokenKey = "firebaseToken";

    public Task SaveTokenAsync(string token) =>
        SecureStorage.SetAsync(TokenKey, token);

    public Task<string> GetTokenAsync() =>
        SecureStorage.GetAsync(TokenKey);

    public Task ClearTokenAsync() =>
        SecureStorage.SetAsync(TokenKey, null); // ← Så här rensar du
}
