using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static lek4.Components.Pages.Login;

public class UserService
{
    private readonly HttpClient _httpClient;

    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task UpdateUserProfile(string email)
    {
        // Hämta den nuvarande JSON-filen från Firebase Storage
        var response = await _httpClient.GetAsync($"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2F{email}.json");

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response Body: {responseBody}");

            try
            {
                var userProfile = JsonSerializer.Deserialize<UserProfile>(responseBody);

                if (userProfile != null)
                {
                    // Uppdatera IsAdmin till true
                    userProfile.IsAdmin = true;

                    // Serialisera tillbaka till JSON
                    var updatedJson = JsonSerializer.Serialize(userProfile);

                    // Ladda upp den uppdaterade JSON-filen tillbaka till Firebase Storage
                    var content = new StringContent(updatedJson, Encoding.UTF8, "application/json");
                    var putResponse = await _httpClient.PutAsync($"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2F{email}.json", content);

                    if (putResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine("User profile updated successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to update user profile. Status code: {putResponse.StatusCode}");
                    }
                }
                else
                {
                    Console.WriteLine("Failed to deserialize user profile.");
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON deserialization error: {ex.Message}");
            }
        }
    }

    public class UserProfile
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public bool IsAdmin { get; set; } // Korrekt egenskap
    }
}
