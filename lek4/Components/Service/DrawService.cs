using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace lek4.Components.Service
{
    public class DrawService
    {
        private readonly HttpClient _httpClient;

        public DrawService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Klass för att representera en användares lock-in-data
        public class UserLockInData
        {
            public string UserEmail { get; set; }
            public double LockInAmount { get; set; }
        }

        // Hämta alla användares lock-in-data från Firebase för en specifik produkt
        // Fetches all users' lock-in data from Firebase for a specific product
        public async Task<Dictionary<string, double>> GetAllUserLockInData(int productNumber)
        {
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2Ftotalusers.json?alt=media";
            var response = await _httpClient.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                // Deserialize to Dictionary<string, double>
                return JsonSerializer.Deserialize<Dictionary<string, double>>(jsonResponse);
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new Dictionary<string, double>();  // Return an empty dictionary if file doesn't exist
            }
            else
            {
                Console.WriteLine($"Error fetching total users for product {productNumber}: {response.StatusCode}");
                return new Dictionary<string, double>();  // Return an empty dictionary on error
            }
        }



        // Hämta lock-in-data för en specifik användare
        private async Task<UserLockInData> GetUserLockInData(int productNumber, string userFileName)
        {
            var url = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2F{userFileName}?alt=media";

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<UserLockInData>(jsonResponse);
            }
            else
            {
                Console.WriteLine($"Failed to fetch user data for file {userFileName}. Status code: {response.StatusCode}");
                return null;
            }
        }

        // Utför en slumpmässig dragning baserat på lock-in-belopp
        // Perform a random draw based on lock-in amounts
        public string DrawWinner(Dictionary<string, double> users)
        {
            var weightedList = new List<string>();

            // Iterate over each user and add their email multiple times to the list based on lockInAmount
            foreach (var user in users)
            {
                int weight = (int)Math.Ceiling(user.Value);  // Calculate the weight based on lock-in amount
                for (int i = 0; i < weight; i++)
                {
                    weightedList.Add(user.Key);  // Add the user's email to the weighted list
                }
            }

            // Perform the random draw
            Random random = new Random();
            int index = random.Next(weightedList.Count);
            return weightedList[index];  // Return the winner's email
        }


        // Spara vinnaren i Firebase i en separat winner.json-fil
        public async Task SaveWinnerToFirebase(int productNumber, string winnerEmail)
        {
            var winnerData = new
            {
                Winner = winnerEmail,
                Timestamp = DateTime.UtcNow
            };

            var winnerJson = JsonSerializer.Serialize(winnerData);
            var content = new StringContent(winnerJson, Encoding.UTF8, "application/json");

            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fwinner%2Fproduct{productNumber}%2Fwinner.json";

            var response = await _httpClient.PostAsync(path, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Winner {winnerEmail} saved successfully for product {productNumber}.");
            }
            else
            {
                Console.WriteLine($"Failed to save winner for product {productNumber}. Error: {response.StatusCode}");
            }
        }
    }
}
