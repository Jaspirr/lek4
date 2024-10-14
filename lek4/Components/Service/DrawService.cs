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
    private readonly HttpClient _httpClient;
    private readonly ProductService _productService;

    public DrawService(HttpClient httpClient, ProductService productService)
    {
        _httpClient = httpClient;
        _productService = productService;
    }

    // Method to draw a winner from the users locked into a product
    public async Task<string> DrawWinnerAsync(int productNumber)
    {
        try
        {
            // Fetch the locked-in users for the product
            var lockedInUsers = await _productService.GetUsersForProduct(productNumber);

            if (lockedInUsers != null && lockedInUsers.Count > 0)
            {
                // Select a random user as the winner
                var random = new Random();
                var winner = lockedInUsers[random.Next(lockedInUsers.Count)];

                // Return the winner's email
                return winner.UserEmail;
            }
            else
            {
                Console.WriteLine($"No users locked in for product {productNumber}.");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error drawing winner for product {productNumber}: {ex.Message}");
            return null;
        }
    }


    // Save the drawn winner to Firebase under winner.json
    public async Task SaveWinnerToFirebase(int productNumber, string winnerEmail)
    {
        var winnerData = new Dictionary<string, string>
        {
            { "Winner", winnerEmail }
        };

        var winnerJson = JsonSerializer.Serialize(winnerData);
        var content = new StringContent(winnerJson, Encoding.UTF8, "application/json");

        // Firebase path for winner.json under the product folder
        var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/products/{productNumber}/winner.json";

        // Save the winner to Firebase
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

