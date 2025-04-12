using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace lek4.Components.Service
{
    public class BoostFriendService
    {
        private readonly HttpClient _httpClient;
        private readonly UserService _userService;
        private const string BoostFriendPairsUrl = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FDailyRewards%2FBoostFriendPairs.json";
        private const string BoostFriendClaimsUrl = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FDailyRewards%2FBoostFriendClaims.json";
        private const string BoostFriendReceivedUrl = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FDailyRewards%2FBoostFriendReceived.json";

        public BoostFriendService(HttpClient httpClient, UserService userService)
        {
            _httpClient = httpClient;
            _userService = userService;
        }

        public async Task<bool> HasGivenCreditToday(string userEmail)
        {
            var response = await _httpClient.GetAsync($"{BoostFriendClaimsUrl}?alt=media");
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var boostFriendClaims = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(jsonResponse)
                                     ?? new Dictionary<string, Dictionary<string, string>>();

                if (boostFriendClaims.ContainsKey(userEmail) &&
                    boostFriendClaims[userEmail].TryGetValue("ClaimDate", out string claimDate) &&
                    DateTime.Parse(claimDate).Date == DateTime.UtcNow.Date)
                {
                    return true;
                }
            }
            return false;
        }

        public async Task<string> GetSavedFriendEmail(string userEmail)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BoostFriendPairsUrl}?alt=media");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var boostPairsData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(jsonResponse)
                                         ?? new Dictionary<string, Dictionary<string, string>>();

                    if (boostPairsData.ContainsKey(userEmail))
                    {
                        return boostPairsData[userEmail]["FriendEmail"];
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading friend email: {ex.Message}");
            }
            return string.Empty;
        }

        public async Task<(bool Success, string ErrorMessage)> GiveCreditToFriend(string userEmail, string friendEmail)
        {
            if (userEmail.Equals(friendEmail, StringComparison.OrdinalIgnoreCase))
            {
                return (false, "You cannot give credit to yourself.");
            }

            try
            {
                // Hämta den aktuella datan från BoostFriendReceived.json
                var receivedResponse = await _httpClient.GetAsync($"{BoostFriendReceivedUrl}?alt=media");
                var boostFriendReceived = new Dictionary<string, Dictionary<string, string>>();

                if (receivedResponse.IsSuccessStatusCode)
                {
                    var receivedJsonResponse = await receivedResponse.Content.ReadAsStringAsync();
                    boostFriendReceived = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(receivedJsonResponse)
                                           ?? new Dictionary<string, Dictionary<string, string>>();
                }

                // Kolla om användaren redan finns i mottagarlistan
                if (!boostFriendReceived.ContainsKey(friendEmail))
                {
                    boostFriendReceived[friendEmail] = new Dictionary<string, string>
            {
                { "ReceiveDate", DateTime.UtcNow.ToString("yyyy-MM-dd") },
                { "ReceiveCount", "1" },
                { "ReceivedFrom", userEmail },
                { "TotalReceivedCount", "1" }
            };
                }
                else
                {
                    var lastReceiveDate = DateTime.Parse(boostFriendReceived[friendEmail]["ReceiveDate"]);
                    int currentReceiveCount = int.Parse(boostFriendReceived[friendEmail].GetValueOrDefault("ReceiveCount", "0"));
                    int totalReceiveCount = int.Parse(boostFriendReceived[friendEmail].GetValueOrDefault("TotalReceivedCount", "0"));

                    // ✅ Kontrollera om det är en ny dag - Återställ om det är ny dag
                    if (lastReceiveDate.Date != DateTime.UtcNow.Date)
                    {
                        boostFriendReceived[friendEmail]["ReceiveDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd");
                        boostFriendReceived[friendEmail]["ReceiveCount"] = "1";
                        boostFriendReceived[friendEmail]["ReceivedFrom"] = userEmail;
                    }
                    else if (currentReceiveCount < 3)  // ✅ Öka endast om maxgräns ej uppnåtts
                    {
                        boostFriendReceived[friendEmail]["ReceiveCount"] = (currentReceiveCount + 1).ToString();

                        // ✅ Lägg till fler avsändare om redan finns
                        if (!boostFriendReceived[friendEmail]["ReceivedFrom"].Contains(userEmail))
                        {
                            boostFriendReceived[friendEmail]["ReceivedFrom"] += $", {userEmail}";
                        }
                    }
                    else
                    {
                        return (false, "Friend has reached the maximum receive limit for today.");
                    }

                    // ✅ Uppdatera TotalReceivedCount
                    boostFriendReceived[friendEmail]["TotalReceivedCount"] = (totalReceiveCount + 1).ToString();
                }

                // Spara tillbaka den uppdaterade datan
                var updatedReceivedJson = JsonSerializer.Serialize(boostFriendReceived);
                var receivedContent = new StringContent(updatedReceivedJson, Encoding.UTF8, "application/json");
                await _httpClient.PostAsync(BoostFriendReceivedUrl, receivedContent);

                // ✅ Lägg till kredit till användaren
                await _userService.AddCreditToUser(friendEmail, 1);

                return (true, "Credit successfully sent!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error giving credit to a friend: {ex.Message}");
                return (false, "An error occurred while processing the credit transfer.");
            }
        }
    }

    public class CreditResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}
