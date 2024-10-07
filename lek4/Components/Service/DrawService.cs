using lek4.Components.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class DrawService
{
    private readonly ProductService _productService;
    private readonly HttpClient _httpClient;

    public DrawService(ProductService productService, HttpClient httpClient)
    {
        _productService = productService;
        _httpClient = httpClient; // Store HttpClient instance
    }

    // The existing GetWinnerAsync method
    public async Task<string> GetWinnerAsync(int productNumber)
    {
        try
        {
            // Uppdaterad Firebase-URL för att hämta vinnare för en specifik produkt
            var url = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/products%2F{productNumber}%2Fwinner.json?alt=media";
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var winnerData = JsonSerializer.Deserialize<WinnerData>(jsonResponse);

                return $"{winnerData.FirstName} {winnerData.LastName}";
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine($"No winner found for product {productNumber}. Winner has not been drawn yet.");
                return "No winner yet";
            }
            else
            {
                Console.WriteLine($"Failed to fetch winner for product {productNumber}. Status code: {response.StatusCode}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching winner for product {productNumber}: {ex.Message}");
            return null;
        }
    }


    // New DrawWinnerAsync method for drawing the winner
    public async Task<string> DrawWinnerAsync(int productNumber)
    {
        var lockedInUsersWithAmounts = _productService.GetLockedInUsersWithLockInAmount(productNumber);
        if (lockedInUsersWithAmounts.Count > 0)
        {
            var totalLockInAmount = lockedInUsersWithAmounts.Sum(x => x.LockInAmount);
            var randomValue = new Random().NextDouble() * totalLockInAmount;
            double cumulativeOdds = 0.0;

            foreach (var user in lockedInUsersWithAmounts)
            {
                cumulativeOdds += user.LockInAmount;
                if (randomValue <= cumulativeOdds)
                {
                    var winnerUserEmail = user.UserEmail;

                    // Step 3: Save winner to Firebase
                    await SaveWinnerToFirebase(productNumber, winnerUserEmail);

                    // Fetch user profile for display
                    var userProfile = await _productService.GetUserProfileFromFirebase(winnerUserEmail);
                    return userProfile != null ? $"{userProfile.FirstName} {userProfile.LastName}" : winnerUserEmail;
                }
            }
        }
        return "No winner";
    }


    public async Task SaveWinnerToFirebase(int productNumber, string winnerEmail)
    {
        var winnerData = new { Winner = winnerEmail };

        var winnerJson = JsonSerializer.Serialize(winnerData);
        var content = new StringContent(winnerJson, Encoding.UTF8, "application/json");

        // Firebase path for saving the winner
        var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/products%2F{productNumber}%2Fwinner.json";

        // Send PUT request to save winner data
        var response = await _httpClient.PutAsync(path, content);
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Winner for product {productNumber} saved successfully.");
        }
        else
        {
            Console.WriteLine($"Failed to save winner for product {productNumber}. Error: {response.StatusCode}");
        }
    }


}

// WinnerData class to deserialize the winner info
public class WinnerData
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string WinnerEmail { get; set; }
}

