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

    // Method to draw the winner
    public async Task<string> DrawWinnerAsync(int productNumber)
    {
        // Fetch emails and lock-in amounts for the given product
        var lockedInUsersWithAmounts = await _productService.GetLockedInUsersWithLockInAmountAsync(productNumber);

        if (lockedInUsersWithAmounts.Count > 0)
        {
            // Sum of all lock-in amounts
            var totalLockInAmount = lockedInUsersWithAmounts.Sum(x => x.LockInAmount);
            var randomValue = new Random().NextDouble() * totalLockInAmount;
            double cumulativeOdds = 0.0;

            // Select the winner based on lock-in amounts
            foreach (var user in lockedInUsersWithAmounts)
            {
                cumulativeOdds += user.LockInAmount;
                if (randomValue <= cumulativeOdds)
                {
                    var winnerUserEmail = user.UserEmail;

                    // Save the winner to Firebase
                    await SaveWinnerToFirebase(productNumber, winnerUserEmail);

                    // Return winner's email or display name for confirmation
                    return winnerUserEmail;
                }
            }
        }

        return "No winner";
    }




    // Method to fetch the winner from Firebase using ProductService
    public async Task<string> GetWinnerAsync(int productNumber)
    {
        // Delegate the call to ProductService to fetch the winner from Firebase
        return await _productService.GetWinnerFromFirebase(productNumber);
    }

    // Method to save the winner to Firebase
    public async Task SaveWinnerToFirebase(int productNumber, string winnerEmail)
    {
        if (string.IsNullOrEmpty(winnerEmail))
        {
            Console.WriteLine("Error: Cannot save invalid winner.");
            return;
        }

        var winnerData = new { Winner = winnerEmail };
        var winnerJson = JsonSerializer.Serialize(winnerData);
        var content = new StringContent(winnerJson, Encoding.UTF8, "application/json");

        // Firebase path without ?alt=media
        var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fwinner%2F{productNumber}%2Fwinner.json";

        // Log the URL and data for debugging
        Console.WriteLine($"URL: {path}");
        Console.WriteLine($"Data being sent: {winnerJson}");

        // Use PUT request to save winner data
        var response = await _httpClient.PutAsync(path, content);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Winner for product {productNumber} saved successfully.");
        }
        else
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

