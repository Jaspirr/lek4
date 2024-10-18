using System;
using System.Collections.Generic;
using System.Linq;
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
        public async Task<List<UserLockInData>> GetAllUserLockInData(int productNumber)
        {
            var userLockInDataList = new List<UserLockInData>();

            // Hämta användarfilerna från Firebase
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2F?alt=media";

            // Mocka för närvarande att vi hämtar filnamnen för alla användare under produktens mapp
            var userFiles = new List<string> { "user1.json", "user2.json", "user3.json" }; // Du kan använda din metod för att hämta filnamnen

            foreach (var userFile in userFiles)
            {
                var userLockInData = await GetUserLockInData(productNumber, userFile);
                if (userLockInData != null)
                {
                    userLockInDataList.Add(userLockInData);
                }
            }

            return userLockInDataList;
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
        public string DrawWinner(List<UserLockInData> users)
        {
            // Skapa en lista där varje användare läggs in så många gånger som deras LockInAmount
            var weightedList = new List<string>();

            foreach (var user in users)
            {
                int weight = (int)Math.Ceiling(user.LockInAmount);  // Skapa en vikt baserad på lock-in amount
                for (int i = 0; i < weight; i++)
                {
                    weightedList.Add(user.UserEmail);  // Lägg till användaren i listan
                }
            }

            // Skapa en slumpmässig dragning baserat på den viktade listan
            Random random = new Random();
            int index = random.Next(weightedList.Count);
            return weightedList[index];
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
