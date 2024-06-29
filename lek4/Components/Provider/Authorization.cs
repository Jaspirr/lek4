using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class Authorization
{
    private static readonly HttpClient httpClient = new HttpClient();
    private readonly CustomAuthenticationStateProvider _authStateProvider;

    public Authorization(CustomAuthenticationStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            var apiKey = "YOUR_FIREBASE_API_KEY"; // Replace with your Firebase API key
            var requestUri = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";

            var payload = new
            {
                email = email,
                password = password,
                returnSecureToken = true
            };

            var content = new StringContent(JObject.FromObject(payload).ToString(), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(requestUri, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var responseJson = JObject.Parse(responseBody);
            var idToken = responseJson["idToken"].ToString();
            var emailVerified = await IsEmailVerifiedAsync(idToken);

            if (!emailVerified)
            {
                return false;
            }

            _authStateProvider.NotifyUserAuthentication(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SignUpAsync(string email, string password)
    {
        try
        {
            var apiKey = "YOUR_FIREBASE_API_KEY"; // Replace with your Firebase API key
            var requestUri = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={apiKey}";

            var payload = new
            {
                email = email,
                password = password,
                returnSecureToken = true
            };

            var content = new StringContent(JObject.FromObject(payload).ToString(), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(requestUri, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var responseJson = JObject.Parse(responseBody);
            var idToken = responseJson["idToken"].ToString();

            // Send email verification
            await SendEmailVerificationAsync(idToken);

            _authStateProvider.NotifyUserAuthentication(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Logout()
    {
        _authStateProvider.NotifyUserLogout();
    }

    private async Task<bool> IsEmailVerifiedAsync(string idToken)
    {
        var apiKey = "YOUR_FIREBASE_API_KEY"; // Replace with your Firebase API key
        var requestUri = $"https://identitytoolkit.googleapis.com/v1/accounts:lookup?key={apiKey}";

        var payload = new
        {
            idToken = idToken
        };

        var content = new StringContent(JObject.FromObject(payload).ToString(), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(requestUri, content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var responseJson = JObject.Parse(responseBody);
        var users = responseJson["users"] as JArray;

        if (users != null && users.Count > 0)
        {
            return (bool)users[0]["emailVerified"];
        }

        return false;
    }

    private async Task SendEmailVerificationAsync(string idToken)
    {
        var apiKey = "YOUR_FIREBASE_API_KEY"; // Replace with your Firebase API key
        var requestUri = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={apiKey}";

        var payload = new
        {
            requestType = "VERIFY_EMAIL",
            idToken = idToken
        };

        var content = new StringContent(JObject.FromObject(payload).ToString(), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(requestUri, content);
        response.EnsureSuccessStatusCode();
    }
}
