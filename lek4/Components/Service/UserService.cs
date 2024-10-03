using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class UserService
{
    private readonly HttpClient _httpClient;

    // Property to store the current user's email globally
    public string CurrentUserEmail { get; set; }

    // Constructor to initialize the HttpClient
    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Method to set the current user's email after login
    public void SetCurrentUserEmail(string email)
    {
        CurrentUserEmail = email;
    }

    // Method to get the current user's email
    public string GetCurrentUserEmail()
    {
        return CurrentUserEmail;
    }

    // Method to update the user profile in Firebase Storage
    public async Task UpdateUserProfile(string email)
    {
        // Fetch the current JSON file from Firebase Storage
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
                    // Update IsAdmin to true (example)
                    userProfile.IsAdmin = true;

                    // Serialize the updated user profile back to JSON
                    var updatedJson = JsonSerializer.Serialize(userProfile);

                    // Upload the updated JSON file back to Firebase Storage
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
        else
        {
            Console.WriteLine($"Failed to fetch user profile. Status code: {response.StatusCode}");
        }
    }

    // User profile model
    public class UserProfile
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public bool IsAdmin { get; set; } // Property to indicate if the user is an admin
    }
}
