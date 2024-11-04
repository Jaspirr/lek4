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
    private double newCredits;
    // Constructor to initialize the HttpClient
    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Method to set the current user's email after login
    public void SetCurrentUserEmail(string userEmail)
    {
        CurrentUserEmail = userEmail;
    }
    public async Task AddCreditToUser(string userEmail)
    {
        // Trim any surrounding whitespace or quotes from the email
        userEmail = userEmail.Trim().Trim('"');

        if (string.IsNullOrEmpty(userEmail) || userEmail == "Anonymous")
        {
            Console.WriteLine("Invalid email. Cannot add credits for Anonymous user.");
            return;
        }

        // Fetch the existing user data from Firebase (if available)
        var existingUser = await GetUserFromFirebase(userEmail);
        int currentCredits = (int)(existingUser?.Credits ?? 0);
        // Use existing credits or default to 0

        // Increment credits
        currentCredits++;

        // Create or update user data
        var userStats = new
        {
            UserEmail = userEmail,
            LockInAmount = existingUser?.LockInAmount ?? 0.0,  // Preserve the existing LockInAmount
            Credits = currentCredits                           // Incremented Credits
        };

        // Serialize user data to JSON
        var userJson = JsonSerializer.Serialize(userStats);
        var content = new StringContent(userJson, Encoding.UTF8, "application/json");

        // Set the Firebase path for saving user data based on email, adding ?alt=media
        var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FUserStats%2F{userEmail}.json?alt=media";

        // Use POST to save data to Firebase
        var response = await _httpClient.PostAsync(path, content);

        var responseContent = await response.Content.ReadAsStringAsync();  // Log the response for debugging
        Console.WriteLine($"Response content: {responseContent}");

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Credits for {userEmail} updated successfully.");
        }
        else
        {
            Console.WriteLine($"Failed to update credits for {userEmail}. Error: {response.StatusCode}");
        }
    }
    public async Task AddCreditToUser(string userEmail, int creditsToAdd = 1)
    {
        userEmail = userEmail.Trim().Trim('"');
        if (string.IsNullOrEmpty(userEmail) || userEmail == "Anonymous")
        {
            Console.WriteLine("Invalid email. Cannot add credits for Anonymous user.");
            return;
        }

        // Fetch the existing user data from Firebase (if available)
        var existingUser = await GetUserFromFirebase(userEmail);
        int currentCredits = (int)(existingUser?.Credits ?? 0);

        // Increment user's credits
        currentCredits += creditsToAdd;

        // Update user's stats in Firebase
        await UpdateUserCredits(userEmail, currentCredits);

        // Update total credits in Firebase
        await UpdateTotalCredits(creditsToAdd);
    }

    public async Task<double> GetUserCredits()
    {
        if (string.IsNullOrEmpty(CurrentUserEmail))
        {
            throw new InvalidOperationException("Current user email is not set.");
        }

        var response = await _httpClient.GetAsync($"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FUserStats%2F{CurrentUserEmail}.json?alt=media");
        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var userStats = JsonSerializer.Deserialize<UserStats>(responseBody);
            return userStats?.Credits ?? 0.0;
        }
        else
        {
            throw new HttpRequestException("Failed to fetch user stats from Firebase.");
        }
    }

    public async Task<double> GetTotalCredits()
    {
        var path = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FUserStats%2Ftotalcredits.json?alt=media";
        var response = await _httpClient.GetAsync(path);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<double>(responseBody);
        }
        else
        {
            // Return 0 as default if the file doesn't exist
            return 0.0;
        }
    }

    public async Task UpdateTotalCredits(double deltaCredits)
    {
        // Hämta nuvarande total credits
        double currentTotalCredits = await GetTotalCredits();

        // Beräkna ny summa
        double newTotalCredits = currentTotalCredits + deltaCredits;

        // Spara uppdaterad summa
        await SaveTotalCredits(newTotalCredits);
    }

    private async Task SaveTotalCredits(double totalCredits)
    {
        var path = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FUserStats%2Ftotalcredits.json";
        var creditsJson = JsonSerializer.Serialize(totalCredits);
        var content = new StringContent(creditsJson, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync(path, content);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("Failed to update total credits in Firebase.");
        }
    }

    // Helper method to fetch existing user data from Firebase
    public async Task<UserStats?> GetUserFromFirebase(string userEmail)
    {
        var response = await _httpClient.GetAsync($"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FUserStats%2F{userEmail}.json?alt=media");

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserStats>(responseBody);
        }

        Console.WriteLine($"Failed to retrieve user data for {userEmail}. Status code: {response.StatusCode}");
        return null;
    }

    // UserStats class for deserialization
    public class UserStats
    {
        public string UserEmail { get; set; }
        public double LockInAmount { get; set; }
        public int Credits { get; set; }
    }


    // Method to get the current user's email
    public string GetCurrentUserEmail()
    {
        return CurrentUserEmail;
    }

    // Method to update the user profile in Firebase Storage
    public async Task<bool> UpdateUserCredits(string userEmail, int newCredits)
    {
        var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FUserStats%2F{userEmail}.json";

        // Fetch current user stats
        var userStats = await GetUserFromFirebase(userEmail);
        if (userStats == null)
        {
            Console.WriteLine("User stats not found, cannot update credits.");
            return false;
        }

        // Update the credits
        userStats.Credits = newCredits;

        // Serialize updated user stats back to JSON
        var updatedUserJson = JsonSerializer.Serialize(userStats);
        var content = new StringContent(updatedUserJson, Encoding.UTF8, "application/json");

        // Send the updated data to Firebase
        var response = await _httpClient.PutAsync(path, content);
        return response.IsSuccessStatusCode;
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
