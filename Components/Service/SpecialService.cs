using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace lek4.Components.Service
{
    public class SpecialService
    {
        private readonly HttpClient _httpClient;
        private const string SpecialClaimsUrl = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FDailyRewards%2FSpecialClaims.json";
        private const string SpecialInfoUrl = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fconfig%2FSpecialInfo.json";

        public SpecialService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public class SpecialClaim
        {
            public bool HasClaimed { get; set; }
            public string ClaimDate { get; set; }
        }

        public async Task<Dictionary<string, int>> GetSpecialClaims()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{SpecialClaimsUrl}?alt=media");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("No data found, initializing empty dictionary.");
                    return new Dictionary<string, int>();
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Dictionary<string, int>>(jsonResponse)
                       ?? new Dictionary<string, int>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching special claims: {ex.Message}");
                return new Dictionary<string, int>();
            }
        }
        public async Task<bool> HasClaimedSpecialReward(string userEmail)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{SpecialClaimsUrl}?alt=media");

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var claims = JsonSerializer.Deserialize<Dictionary<string, int>>(jsonResponse) ?? new Dictionary<string, int>();

                // Check if the user has already claimed
                return claims.ContainsKey(userEmail) && claims[userEmail] == 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking special claim: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> MarkSpecialRewardAsClaimed(string userEmail)
        {
            try
            {
                // Fetch existing claims
                var response = await _httpClient.GetAsync($"{SpecialClaimsUrl}?alt=media");
                var claims = new Dictionary<string, int>();

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    claims = JsonSerializer.Deserialize<Dictionary<string, int>>(jsonResponse) ?? new Dictionary<string, int>();
                }

                // Mark the user's reward as claimed
                claims[userEmail] = 1;

                // Save updated claims
                var updatedJson = JsonSerializer.Serialize(claims);
                var content = new StringContent(updatedJson, Encoding.UTF8, "application/json");

                var uploadResponse = await _httpClient.PostAsync(SpecialClaimsUrl, content);
                return uploadResponse.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking special reward as claimed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ClaimSpecialReward(string userEmail)
        {
            var claims = await GetSpecialClaims();

            if (claims.ContainsKey(userEmail) && claims[userEmail] == 1)
            {
                Console.WriteLine("User has already claimed this special reward.");
                return false;
            }

            var specialInfo = await GetSpecialInfo();
            int creditsToAward = specialInfo.Credits;

            bool creditsAwarded = await AwardCreditsToUser(userEmail, creditsToAward);

            if (creditsAwarded)
            {
                claims[userEmail] = 1;
                return await SaveSpecialClaims(claims);
            }

            return false;
        }

        private async Task<bool> SaveSpecialClaims(Dictionary<string, int> claims)
        {
            try
            {
                var jsonData = JsonSerializer.Serialize(claims, new JsonSerializerOptions { WriteIndented = true });
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(SpecialClaimsUrl, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving special claims: {ex.Message}");
                return false;
            }
        }

        public async Task<SpecialInfo> GetSpecialInfo()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{SpecialInfoUrl}?alt=media");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<SpecialInfo>(jsonResponse) ?? new SpecialInfo();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Special Info: {ex.Message}");
            }
            return new SpecialInfo();
        }
       
        public async Task<bool> IncrementLinkClickCounter()
        {
            try
            {
                var specialInfo = await GetSpecialInfo();
                specialInfo.LinkClicks += 1;
                return await SaveSpecialInfo(specialInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error incrementing link clicks: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SaveSpecialInfo(SpecialInfo specialInfo)
        {
            try
            {
                var jsonData = JsonSerializer.Serialize(specialInfo, new JsonSerializerOptions { WriteIndented = true });
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(SpecialInfoUrl, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving special info: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AwardCreditsToUser(string userEmail, int credits)
        {
            var userCreditsUrl = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FUserStats%2F{Uri.EscapeDataString(userEmail)}.json";

            try
            {
                var response = await _httpClient.GetAsync($"{userCreditsUrl}?alt=media");
                var userData = new Dictionary<string, object>();

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(jsonResponse))
                    {
                        userData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse) ?? new Dictionary<string, object>();
                    }
                }

                if (userData.ContainsKey("Credits") && userData["Credits"] is JsonElement jsonElement && jsonElement.TryGetInt32(out int currentCredits))
                {
                    userData["Credits"] = currentCredits + credits;
                }
                else
                {
                    userData["Credits"] = credits;
                }

                var updatedJson = JsonSerializer.Serialize(userData, new JsonSerializerOptions { WriteIndented = true });
                var content = new StringContent(updatedJson, Encoding.UTF8, "application/json");
                var uploadResponse = await _httpClient.PostAsync(userCreditsUrl, content);
                return uploadResponse.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error awarding credits to {userEmail}: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> ClearSpecialInfo()
        {
            try
            {
                // Reset SpecialInfo to default values
                var defaultSpecialInfo = new SpecialInfo
                {
                    Name = "Default Special",
                    Description = "This is a default special event.",
                    Credits = 10,
                    LinkClicks = 0,
                    Link = "",
                    ImageUrl = ""
                };

                var jsonData = JsonSerializer.Serialize(defaultSpecialInfo, new JsonSerializerOptions { WriteIndented = true });
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(SpecialInfoUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("SpecialInfo.json has been reset.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting SpecialInfo.json: {ex.Message}");
            }

            return false;
        }

        public async Task<bool> ClearSpecialClaims()
        {
            try
            {
                // Reset SpecialClaims to an empty dictionary
                var emptyClaims = new Dictionary<string, int>();

                var jsonData = JsonSerializer.Serialize(emptyClaims, new JsonSerializerOptions { WriteIndented = true });
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(SpecialClaimsUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("SpecialClaims.json has been reset.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting SpecialClaims.json: {ex.Message}");
            }

            return false;
        }

    }

    public class SpecialInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int Credits { get; set; } = 0;
        public int LinkClicks { get; set; } = 0;
        public string Link { get; set; } = string.Empty;
    }
}
