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
            // Steg 1: Räkna ut den totala LockInAmount
            var totalLockInAmount = lockedInUsersWithAmounts.Sum(x => x.LockInAmount);

            // Steg 2: Generera ett slumpmässigt värde mellan 0 och totalLockInAmount
            var randomValue = new Random().NextDouble() * totalLockInAmount;

            double cumulativeOdds = 0.0;

            // Steg 3: Iterera över användarna och hitta den som vinner
            foreach (var user in lockedInUsersWithAmounts)
            {
                cumulativeOdds += user.LockInAmount;

                // När cumulativeOdds överskrider randomValue, välj den användaren
                if (randomValue <= cumulativeOdds)
                {
                    var winnerUserEmail = user.UserEmail;

                    // Steg 4: Spara vinnaren i Firebase
                    await SaveWinnerToFirebase(productNumber, winnerUserEmail);

                    // Hämta användarens profil för att visa namn istället för e-post
                    var userProfile = await _productService.GetUserProfileFromFirebase(winnerUserEmail);

                    // Om profilen hittas, returnera namn, annars returnera e-post
                    return userProfile != null
                        ? $"{userProfile.FirstName} {userProfile.LastName}"
                        : winnerUserEmail;
                }
            }
        }

        return "No winner";
    }


    public async Task SaveWinnerToFirebase(int productNumber, string winnerUserEmail)
    {
        var winnerData = new
        {
            WinnerEmail = winnerUserEmail,
            TimeOfDraw = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(winnerData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Uppdaterad path för att spara vinnaren under produktspecifik väg
        var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/products%2F{productNumber}%2Fwinner.json";
        var response = await _httpClient.PutAsync(path, content);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Failed to save winner for product {productNumber}. Status code: {response.StatusCode}");
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

