using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

public class Authorization
{
    private readonly HttpClient _httpClient;
    private readonly CustomAuthenticationStateProvider _authStateProvider;

    public Authorization(HttpClient httpClient, CustomAuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient; // Use the injected instance of HttpClient configured to connect to your Web API
        _authStateProvider = authStateProvider;
    }

    public async Task<bool> AdminLoginAsync(string email, string password)
    {
        try
        {
            // Define the payload for admin login
            var loginModel = new { Email = email, Password = password };

            // Send the login request to your Web API
            var response = await _httpClient.PostAsJsonAsync("/Auth/admin-login", loginModel);

            if (response.IsSuccessStatusCode)
            {
                // Parse the response from the Web API
                var responseBody = await response.Content.ReadFromJsonAsync<LoginResponse>();

                // Check if the login was successful and the user is an admin
                if (responseBody != null && responseBody.IsAdmin)
                {
                    // Notify the app about the successful admin authentication
                    _authStateProvider.NotifyUserAuthentication(email, isAdmin: true);
                    return true; // Return true indicating admin login was successful
                }
            }

            // If the login fails, return false
            return false;
        }
        catch (Exception ex)
        {
            // Handle exceptions, such as network errors
            Console.WriteLine($"An error occurred during login: {ex.Message}");
            return false;
        }
    }
}

public class LoginResponse
{
    public string Message { get; set; }
    public bool IsAdmin { get; set; }
}

