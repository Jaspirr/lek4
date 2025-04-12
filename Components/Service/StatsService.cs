using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace lek4.Components.Service
{
    public class StatsService
    {
        private readonly HttpClient _httpClient;

        public StatsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Klass för att representera totalstats
        public class TotalStats
        {
            public int TotalLockInUsers { get; set; }
            public double TotalLockInAmount { get; set; }
        }
          private async Task<Dictionary<string, double>> GetTotalUsersFromFirebase(int productNumber)
        {
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2Ftotalusers.json?alt=media";
            var response = await _httpClient.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Dictionary<string, double>>(jsonResponse);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new Dictionary<string, double>();  // Returnera en tom dictionary om filen inte existerar
            }
            else
            {
                Console.WriteLine($"Error fetching total users for product {productNumber}: {response.StatusCode}");
                return new Dictionary<string, double>();  // Returnera en tom dictionary vid fel
            }
        }
        // Hämta totalstats för en specifik produkt från Firebase
        public async Task<TotalStats> GetTotalStatsFromFirebase(int productNumber)
        {
            var url = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2Ftotalstats.json?alt=media";
            try
            {
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<TotalStats>(jsonResponse);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new TotalStats { TotalLockInUsers = 0, TotalLockInAmount = 0 };
                }
                else
                {
                    Console.WriteLine($"Failed to fetch total stats for product {productNumber}. Status code: {response.StatusCode}");
                    return new TotalStats { TotalLockInUsers = 0, TotalLockInAmount = 0 };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching total stats for product {productNumber}: {ex.Message}");
                return new TotalStats { TotalLockInUsers = 0, TotalLockInAmount = 0 };
            }
        }

        // Hämta totalusers data från Firebase


        // Uppdatera totalstats för en produkt genom att addera en användares lock-in
        public async Task UpdateTotalStats(int productNumber, string userEmail, double lockInAmount)
        {
            // Hämta de nuvarande totalstats från Firebase
            var totalStats = await GetTotalStatsFromFirebase(productNumber);

            // Hämta totalusers filen för att få alla användares lock-in data
            var totalUsers = await GetTotalUsersFromFirebase(productNumber);

            // Kontrollera om användaren redan finns i totalUsers
            if (totalUsers.ContainsKey(userEmail))
            {
                // Om användaren finns, uppdatera deras lock-in amount
                totalUsers[userEmail] = lockInAmount;
            }
            else
            {
                // Om användaren inte finns, lägg till dem i totalUsers
                totalUsers[userEmail] = lockInAmount;

                // Öka antalet unika användare när en ny användare läggs till
                totalStats.TotalLockInUsers += 1;
            }

            // Uppdatera totalusers filen på Firebase
            var usersJson = JsonSerializer.Serialize(totalUsers, new JsonSerializerOptions { WriteIndented = false });
            var usersContent = new StringContent(usersJson, Encoding.UTF8, "application/json");
            var usersPath = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2Ftotalusers.json";
            await _httpClient.PostAsync(usersPath, usersContent);

            // Summera alla lock-in amounts från totalUsers och uppdatera TotalLockInAmount
            totalStats.TotalLockInAmount = totalUsers.Values.Sum();

            // Uppdatera TotalLockInUsers med antalet unika användare
            totalStats.TotalLockInUsers = totalUsers.Count; // Uppdatera med antalet användare i totalUsers

            // Serialisera de uppdaterade totalstats till JSON och spara på Firebase
            var statsJson = JsonSerializer.Serialize(totalStats);
            var statsContent = new StringContent(statsJson, Encoding.UTF8, "application/json");
            var statsPath = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2Ftotalstats.json";

            try
            {
                // Försök att använda POST för att uppdatera totalstats.json
                var response = await _httpClient.PostAsync(statsPath, statsContent);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Total stats for product {productNumber} updated successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to update total stats for product {productNumber}. Error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating total stats for product {productNumber}: {ex.Message}");
            }
        }


        // Nollställ totalstats för en produkt (t.ex. efter att en produkt har löpt ut eller avslutats)
        public async Task ResetTotalStats(int productNumber)
        {
            var totalStats = new TotalStats
            {
                TotalLockInUsers = 0,
                TotalLockInAmount = 0
            };

            var statsJson = JsonSerializer.Serialize(totalStats);
            var content = new StringContent(statsJson, Encoding.UTF8, "application/json");

            // Skicka PUT-förfrågan för att nollställa totalstats i Firebase
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2Ftotalstats.json";

            try
            {
                var response = await _httpClient.PutAsync(path, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Total stats for product {productNumber} reset successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to reset total stats for product {productNumber}. Error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting total stats for product {productNumber}: {ex.Message}");
            }
        }
    }
}
