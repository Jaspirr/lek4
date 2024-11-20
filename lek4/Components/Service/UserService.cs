using lek4.Components.Service;
using Microsoft.JSInterop;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static lek4.Components.Pages.PriceConfig;

public class UserService
{
    private readonly HttpClient _httpClient;

    // Property to store the current user's email globally
    public string CurrentUserEmail { get; set; }
    private double newCredits;

    private static UserService _instance;
    public static UserService Instance => _instance ?? throw new InvalidOperationException("UserService is not initialized.");


    // Constructor to initialize the HttpClient
    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    public static void InitializeService(HttpClient httpClient)
    {
        _instance = new UserService(httpClient);
    }
    // Method to set the current user's email after login
    public void SetCurrentUserEmail(string userEmail)
    {
        CurrentUserEmail = userEmail;
    }
    public async Task AddCreditToUser(string userEmail)
    {
        // Trimma eventuella vita tecken eller citationstecken från e-posten
        userEmail = userEmail.Trim().Trim('"');

        if (string.IsNullOrEmpty(userEmail) || userEmail == "Anonymous")
        {
            Console.WriteLine("Invalid email. Cannot add credits for Anonymous user.");
            return;
        }

        // Hämta den befintliga användardatan från Firebase (om tillgänglig)
        var existingUser = await GetUserFromFirebase(userEmail);
        int currentCredits = (int)(existingUser?.Credits ?? 0);

        // Öka användarens kredit med 1
        currentCredits++;

        // Skapa eller uppdatera användardatan
        var userStats = new
        {
            UserEmail = userEmail,
            LockInAmount = existingUser?.LockInAmount ?? 0.0,  // Behåll det befintliga LockInAmount
            Credits = currentCredits                           // Uppdaterad Credits
        };

        // Serialisera användardatan till JSON
        var userJson = JsonSerializer.Serialize(userStats);
        var content = new StringContent(userJson, Encoding.UTF8, "application/json");

        // Sätt Firebase-sökvägen för att spara användardata baserat på e-post, med ?alt=media
        var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FUserStats%2F{userEmail}.json?alt=media";

        // Använd POST för att spara data till Firebase
        var response = await _httpClient.PostAsync(path, content);

        var responseContent = await response.Content.ReadAsStringAsync();  // Logga svaret för felsökning
        Console.WriteLine($"Response content: {responseContent}");

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Credits for {userEmail} updated successfully.");

            // Uppdatera totalcredits.json med den nya krediten
            await AddToTotalCredits(1); // Lägg till 1 kredit till den totala summan
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
    public async Task AddToTotalCredits(double creditAmount)
    {
        // GET-förfrågan med ?alt=media för att hämta nuvarande total credits
        var getPath = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FUserStats%2Ftotalcredits.json?alt=media";
        // PUT-förfrågan utan ?alt=media för att uppdatera total credits
        var putPath = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FUserStats%2Ftotalcredits.json";

        try
        {
            // Hämta nuvarande värde av totalCredits från Firebase
            double currentTotalCredits = 0;
            var response = await _httpClient.GetAsync(getPath);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var totalCreditsData = JsonSerializer.Deserialize<Dictionary<string, double>>(responseBody);

                if (totalCreditsData != null && totalCreditsData.ContainsKey("totalCredits"))
                {
                    currentTotalCredits = totalCreditsData["totalCredits"];
                }

                Console.WriteLine($"Current total credits: {currentTotalCredits}");
            }
            else
            {
                Console.WriteLine("totalcredits.json does not exist, starting with 0.");
            }

            // Lägg till det nya kreditbeloppet
            currentTotalCredits += creditAmount;
            Console.WriteLine($"New total credits after addition: {currentTotalCredits}");

            // Skapa JSON-objekt för uppladdning
            var updatedTotalCreditsData = new { totalCredits = currentTotalCredits };
            var totalCreditsJson = JsonSerializer.Serialize(updatedTotalCreditsData);
            Console.WriteLine($"Serialized totalCredits JSON for upload: {totalCreditsJson}");

            var content = new StringContent(totalCreditsJson, Encoding.UTF8, "application/json");

            // Skicka PUT-begäran utan ?alt=media
            var putResponse = await _httpClient.PostAsync(putPath, content);
            if (!putResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to update totalCredits. Status Code: {putResponse.StatusCode}");
                var putResponseContent = await putResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Response content: {putResponseContent}"); // Logga svaret för ytterligare felsökning
            }
            else
            {
                Console.WriteLine("Total credits updated successfully in totalcredits.json.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating totalCredits: {ex.Message}");
        }
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

            try
            {
                // Försök att deserialisera direkt som en double
                var totalCreditsData = JsonSerializer.Deserialize<Dictionary<string, double>>(responseBody);

                if (totalCreditsData != null && totalCreditsData.ContainsKey("totalCredits"))
                {
                    return totalCreditsData["totalCredits"];
                }
                else
                {
                    Console.WriteLine("Key 'totalCredits' not found in JSON data.");
                }
            }
            catch (JsonException jsonEx)
            {
                // Logga JSON-data för felsökning
                Console.WriteLine($"Failed to deserialize JSON to double. JSON content: {responseBody}");
                Console.WriteLine($"Deserialization error: {jsonEx.Message}");
            }
        }
        else
        {
            Console.WriteLine($"Failed to fetch total credits. Status code: {response.StatusCode}");
        }

        // Returnera 0 som standard om något går fel
        return 0.0;
    }


    public async Task UpdateTotalCredits(int creditChange)
    {
        var path = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FUserStats%2Ftotalcredits.json";

        try
        {
            // Hämta nuvarande värde av totalCredits från Firebase
            var response = await _httpClient.GetAsync(path + "?alt=media");

            double currentTotalCredits;
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                currentTotalCredits = JsonSerializer.Deserialize<double>(responseBody);
            }
            else
            {
                // Om filen inte existerar, sätt currentTotalCredits till 0
                currentTotalCredits = 0;
            }

            // Uppdatera totalCredits med förändringen
            currentTotalCredits += creditChange;

            // Spara det nya värdet av totalCredits i totalcredits.json
            var totalCreditsJson = JsonSerializer.Serialize(currentTotalCredits);
            var content = new StringContent(totalCreditsJson, Encoding.UTF8, "application/json");

            var putResponse = await _httpClient.PutAsync(path, content);
            if (!putResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to update totalCredits. Status Code: {putResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating totalCredits: {ex.Message}");
        }
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

        // Hämta nuvarande användardata
        var userStats = await GetUserFromFirebase(userEmail);
        if (userStats == null)
        {
            Console.WriteLine("User stats not found, cannot update credits.");
            return false;
        }

        // Uppdatera credits
        userStats.Credits = newCredits;

        // Serialisera och skicka uppdaterad data till Firebase
        var updatedUserJson = JsonSerializer.Serialize(userStats);
        var content = new StringContent(updatedUserJson, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(path, content);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Failed to update credits for {userEmail}. Response: {response.StatusCode}");
            return false;
        }
        return true;
    }

    public async Task LockInJackpotAsync(string userEmail, JackpotService jackpotService)
    {
        const int lockInCost = 100;

        // 1. Kontrollera och uppdatera användarens krediter
        var userStats = await GetUserFromFirebase(userEmail);
        if (userStats == null || userStats.Credits < lockInCost)
        {
            Console.WriteLine("Du har inte tillräckligt med credits för att delta i jackpotten.");
            return;
        }

        // Dra av kostnaden från användarens credits
        userStats.Credits -= lockInCost;
        bool updateCreditsSuccess = await UpdateUserCredits(userEmail, userStats.Credits);
        if (!updateCreditsSuccess)
        {
            Console.WriteLine("Misslyckades med att uppdatera användarens credits.");
            return;
        }
        Console.WriteLine($"Credits för {userEmail} har uppdaterats till {userStats.Credits}.");

        // 2. Uppdatera JackpotTotalLockin.json med användarens e-post och låst belopp
        await jackpotService.UpdateJackpotTotalLockin(userEmail, lockInCost);
    }
    public async Task<Dictionary<string, Dictionary<string, bool>>> GetOrCreateClaimedPrizes(string claimDataUrl)
    {
        var claimedPrizes = new Dictionary<string, Dictionary<string, bool>>();

        try
        {
            var response = await _httpClient.GetAsync(claimDataUrl);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                claimedPrizes = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, bool>>>(responseBody)
                                ?? new Dictionary<string, Dictionary<string, bool>>();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine("ClaimedPrizes file not found. Initializing empty structure.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching ClaimedPrizes: {ex.Message}");
        }

        return claimedPrizes;
    }
    public async Task SaveClaimedPrizes(Dictionary<string, Dictionary<string, bool>> claimedPrizes)
    {
        var url = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FJackpot%2FClaimedPrizes.json";
        var jsonData = JsonSerializer.Serialize(claimedPrizes, new JsonSerializerOptions { WriteIndented = true });
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("ClaimedPrizes saved successfully.");
            }
            else
            {
                Console.WriteLine($"Failed to save ClaimedPrizes: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SaveClaimedPrizes: {ex.Message}");
        }
    }
    [JSInvokable("ClaimPrizeFromService")]
    public static async Task<bool> ClaimPrizeFromService(string userEmail, string prizeType, double prizeValue)
    {
        // Using the singleton instance
        return await UserService.Instance.ProcessClaimPrize(userEmail, prizeType, prizeValue);
    }
    public async Task<bool> ProcessClaimPrize(string userEmail, string prizeType, double prizeValue)
    {
        if (string.IsNullOrEmpty(userEmail))
        {
            Console.WriteLine("User email is empty or null. Cannot process claim.");
            return false;
        }

        try
        {
            // Om priset är Credits, uppdatera användarens krediter
            if (prizeType == "Credits")
            {
                await AddCreditToUser(userEmail, (int)prizeValue);
                Console.WriteLine($"Added {prizeValue} credits to user {userEmail}.");
                return true;
            }

            // Här kan du lägga till logik för andra typer av priser om det behövs
            Console.WriteLine($"Processing other prize types is not implemented yet: {prizeType}.");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing claim for {userEmail}: {ex.Message}");
            return false;
        }
    }

    public async Task AwardPrizeToUser(string userEmail, OutcomeConfiguration prizeOutcome)
    {
        // Check if prize is credits and PrizeValue is greater than 0
        if (prizeOutcome.PrizeType == "Credits" && prizeOutcome.PrizeValue > 0)
        {
            // Fetch the user's current stats from Firebase
            var existingUser = await GetUserFromFirebase(userEmail);
            if (existingUser == null)
            {
                Console.WriteLine($"User {userEmail} not found.");
                return;
            }

            // Add the prize credits to the user’s existing credits
            existingUser.Credits += (int)prizeOutcome.PrizeValue;

            // Update the user’s stats in Firebase
            bool updateSuccess = await UpdateUserCredits(userEmail, existingUser.Credits);
            if (updateSuccess)
            {
                Console.WriteLine($"Awarded {prizeOutcome.PrizeValue} credits to {userEmail}. New total: {existingUser.Credits}");
            }
            else
            {
                Console.WriteLine($"Failed to update credits for {userEmail}.");
            }
        }
        else
        {
            Console.WriteLine("No credits awarded, as the prize type is not 'Credits' or the value is zero.");
        }
    }
    public class OutcomeConfiguration
    {
        public int CorrectAnswersRequired { get; set; }
        public string Message { get; set; }
        public string IconClass { get; set; }
        public string BackgroundColor { get; set; }
        public string AnimationClass { get; set; }
        public string PrizeType { get; set; }
        public double PrizeValue { get; set; }
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
