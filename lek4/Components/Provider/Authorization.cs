using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Newtonsoft.Json.Linq;

public class Authorization
{
    private readonly HttpClient _httpClient; // Använd en instans av HttpClient som injiceras
    private readonly CustomAuthenticationStateProvider _authStateProvider;

    public Authorization(HttpClient httpClient, CustomAuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient; // Använd den injicerade instansen
        _authStateProvider = authStateProvider;
    }

    public async Task<bool> AdminLoginAsync(string email, string password)
    {
        try
        {
            var apiKey = "AIzaSyCyLKylikL5dUKQEKxMn6EkY6PnBWKmJtA"; // Replace with your Firebase API key
            var requestUri = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";

            var payload = new
            {
                email = email,
                password = password,
                returnSecureToken = true
            };

            var content = new StringContent(JObject.FromObject(payload).ToString(), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(requestUri, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var responseJson = JObject.Parse(responseBody);
            var idToken = responseJson["idToken"].ToString();
            var emailVerified = await IsEmailVerifiedAsync(idToken);

            if (!emailVerified)
            {
                return false;
            }

            // Kontrollera om användaren är admin
            var isAdmin = await IsAdmin(email);

            // Använd NotifyUserAuthentication med både email och isAdmin
            _authStateProvider.NotifyUserAuthentication(email, isAdmin);
            return isAdmin; // Returnera true om användaren är admin
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> IsAdmin(string email)
    {
        var response = await _httpClient.GetAsync($"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2F{email}.json");
        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var userProfile = JsonSerializer.Deserialize<UserProfile>(responseBody);

            return userProfile?.IsAdmin == true;
        }
        return false;
    }

    private async Task<bool> IsEmailVerifiedAsync(string idToken)
    {
        var apiKey = "AIzaSyCyLKylikL5dUKQEKxMn6EkY6PnBWKmJtA"; // Replace with your Firebase API key
        var requestUri = $"https://identitytoolkit.googleapis.com/v1/accounts:lookup?key={apiKey}";

        var payload = new
        {
            idToken = idToken
        };

        var content = new StringContent(JObject.FromObject(payload).ToString(), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(requestUri, content);
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
        var apiKey = "AIzaSyCyLKylikL5dUKQEKxMn6EkY6PnBWKmJtA"; // Replace with your Firebase API key
        var requestUri = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={apiKey}";

        var payload = new
        {
            requestType = "VERIFY_EMAIL",
            idToken = idToken
        };

        var content = new StringContent(JObject.FromObject(payload).ToString(), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(requestUri, content);
        response.EnsureSuccessStatusCode();
    }

    public class UserProfile
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public bool IsAdmin { get; set; }
    }
}
