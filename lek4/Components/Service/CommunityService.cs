using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace lek4.Components.Service
{
    public class CommunityService
    {
        private readonly HttpClient _httpClient;
        private const string CommunityClaimsUrl = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FDailyRewards%2FCommunityClaims.json";
        private const string CommunityInfoUrl = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fconfig%2FCommunityInfo.json";
        private const string ButtonVisibilityConfigUrl = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fconfig%2FButtonVisibilityConfig.json";
        private const string savedInfoBaseUrl = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FSavedInfo%2F";
        public CommunityService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public class CommunityClaim
        {
            public bool HasClaimed { get; set; }
            public string ClaimDate { get; set; }
        }

        /// Hämta användarnas claims från Firebase
        public async Task<Dictionary<string, int>> GetCommunityClaims()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{CommunityClaimsUrl}?alt=media");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("No data found, initializing empty dictionary.");
                    return new Dictionary<string, int>();
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();

                // Enklare deserialisering av formatet { "email": 1 }
                return JsonSerializer.Deserialize<Dictionary<string, int>>(jsonResponse)
                       ?? new Dictionary<string, int>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching community claims: {ex.Message}");
                return new Dictionary<string, int>();
            }
        }
        public async Task<bool> ValidateCommunityCode(string userEmail, string userInputCode)
        {
            try
            {
                // Hämta community info direkt från Firebase
                var communityInfo = await GetCommunityInfo();

                if (string.IsNullOrEmpty(communityInfo.Code))
                {
                    Console.WriteLine("No code found in Firebase.");
                    return false;
                }

                // Jämför koden, trimma och ignorera gemener
                if (!userInputCode.Trim().Equals(communityInfo.Code.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Invalid code entered.");
                    return false;
                }

                // Kontrollera om användaren redan har claimat
                if (await HasClaimedCommunityReward(userEmail))
                {
                    Console.WriteLine("User already claimed the reward.");
                    return false;
                }

                // Claima reward
                return await ClaimCommunityReward(userEmail);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating code: {ex.Message}");
                return false;
            }
        }
        public async Task ClearCommunityInfo()
        {
            try
            {
                // Create a default CommunityInfo object
                var defaultInfo = new CommunityInfo
                {
                    Name = "Default Community",
                    Description = "This is a default community event.",
                    Credits = 10,
                    Duration = 14,
                    Code = "DEFAULTCODE",
                    Link = "https://example.com",
                    ImageUrl = "",
                    StartDate = DateTime.UtcNow
                };

                // Serialize the default info and save it
                var jsonData = JsonSerializer.Serialize(defaultInfo, new JsonSerializerOptions { WriteIndented = true });
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                await _httpClient.PostAsync(CommunityInfoUrl, content);

                Console.WriteLine("CommunityInfo.json has been reset.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting CommunityInfo.json: {ex.Message}");
            }
        }

        public async Task ClearCommunityClaims()
        {
            try
            {
                // Create an empty dictionary
                var emptyClaims = new Dictionary<string, int>();

                // Serialize and save the empty dictionary
                var jsonData = JsonSerializer.Serialize(emptyClaims, new JsonSerializerOptions { WriteIndented = true });
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                await _httpClient.PostAsync(CommunityClaimsUrl, content);

                Console.WriteLine("CommunityClaims.json has been reset.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting CommunityClaims.json: {ex.Message}");
            }
        }

        /// Spara claims till Firebase
        private async Task<bool> SaveCommunityClaims(Dictionary<string, int> claims)
        {
            try
            {
                var jsonData = JsonSerializer.Serialize(claims, new JsonSerializerOptions { WriteIndented = true });
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(CommunityClaimsUrl, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving community claims: {ex.Message}");
                return false;
            }
        }

        /// Kontrollera om användaren redan claimat
        public async Task<bool> HasClaimedCommunityReward(string userEmail)
        {
            var claims = await GetCommunityClaims();
            // Enklare kontroll: Om nyckeln finns och värdet är 1, har de claimat
            return claims.ContainsKey(userEmail) && claims[userEmail] == 1;
        }
        /// Claima community reward
        public async Task<bool> ClaimCommunityReward(string userEmail)
        {
            var claims = await GetCommunityClaims();

            // Kontrollera om användaren redan har claimat
            if (claims.ContainsKey(userEmail) && claims[userEmail] == 1)
            {
                Console.WriteLine("User has already claimed this reward.");
                return false;
            }

            // Hämta community info för att få credits (OBS: Bara för att läsa värdet)
            var communityInfo = await GetCommunityInfo();
            int creditsToAward = communityInfo.Credits;

            // Tilldela krediter till användaren
            bool creditsAwarded = await AwardCreditsToUser(userEmail, creditsToAward);

            if (creditsAwarded)
            {
                // Uppdatera endast claims-filen, inte CommunityInfo.json
                claims[userEmail] = 1;
                return await SaveCommunityClaims(claims);
            }

            return false;
        }

        /// Metod för att skicka credits
        public async Task<bool> AwardCreditsToUser(string userEmail, int credits)
        {
            var userCreditsUrl = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FUserStats%2F{Uri.EscapeDataString(userEmail)}.json";

            try
            {
                // Hämta användarens data från Firebase
                var response = await _httpClient.GetAsync($"{userCreditsUrl}?alt=media");
                var userData = new Dictionary<string, object>(); // Flexibel för att hantera olika datatyper

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    // Deserialisera hela användarens data (om filen inte är tom)
                    if (!string.IsNullOrWhiteSpace(jsonResponse))
                    {
                        userData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse) ?? new Dictionary<string, object>();
                    }
                }

                // Kontrollera och uppdatera "Credits" fältet
                if (userData.ContainsKey("Credits") && userData["Credits"] is JsonElement jsonElement && jsonElement.TryGetInt32(out int currentCredits))
                {
                    userData["Credits"] = currentCredits + credits;
                }
                else
                {
                    // Om "Credits" inte finns, lägg till det
                    userData["Credits"] = credits;
                }

                // Serialisera och spara tillbaka till Firebase
                var updatedJson = JsonSerializer.Serialize(userData, new JsonSerializerOptions { WriteIndented = true });
                var content = new StringContent(updatedJson, Encoding.UTF8, "application/json");

                var uploadResponse = await _httpClient.PostAsync(userCreditsUrl, content);
                if (uploadResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Successfully awarded {credits} credits to {userEmail}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Failed to update credits for {userEmail}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating credits for {userEmail}: {ex.Message}");
                return false;
            }
        }

        /// Hämta community info från Firebase
        public async Task<CommunityInfo> GetCommunityInfo()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{CommunityInfoUrl}?alt=media");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<CommunityInfo>(jsonResponse) ?? new CommunityInfo();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    var defaultInfo = new CommunityInfo
                    {
                        Name = "Default Community",
                        Description = "This is a default community event.",
                        Credits = 10,
                        Duration = 14,
                        Code = "DEFAULTCODE",
                        Link = "https://example.com",
                        ImageUrl = "",
                        StartDate = DateTime.UtcNow
                    };
                    await SaveCommunityInfo(defaultInfo);
                    return defaultInfo;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching community info: {ex.Message}");
            }
            return new CommunityInfo();
        }

        /// Spara community info till Firebase
        public async Task<bool> SaveCommunityInfo(CommunityInfo communityInfo)
        {
            try
            {
                var jsonData = JsonSerializer.Serialize(communityInfo, new JsonSerializerOptions { WriteIndented = true });
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(CommunityInfoUrl, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving community info: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> IncrementLinkClickCounter()
        {
            try
            {
                // Hämta den senaste community-infon
                var communityInfo = await GetCommunityInfo();

                // Öka antalet klick
                communityInfo.LinkClicks += 1;

                // Spara uppdaterad info tillbaka till Firebase
                return await SaveCommunityInfo(communityInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error incrementing link clicks: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> UpdateButtonVisibilityAsync(string buttonName, bool visibility)
        {
            try
            {
                // Fetch existing visibility config
                var response = await _httpClient.GetAsync($"{ButtonVisibilityConfigUrl}?alt=media");
                var config = new Dictionary<string, bool>();

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    config = JsonSerializer.Deserialize<Dictionary<string, bool>>(json) ?? new Dictionary<string, bool>();
                }

                // Update visibility
                if (config.ContainsKey(buttonName))
                {
                    config[buttonName] = visibility;
                }
                else
                {
                    config.Add(buttonName, visibility);
                }

                // Save updated configuration
                var jsonData = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var saveResponse = await _httpClient.PostAsync(ButtonVisibilityConfigUrl, content);

                if (saveResponse.IsSuccessStatusCode)
                {
                    // Check and trigger save summary
                    await TriggerSaveSummaryIfNeeded();
                }

                return saveResponse.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating button visibility: {ex.Message}");
                return false;
            }
        }
        private async Task TriggerSaveSummaryIfNeeded()
        {
            try
            {
                // Load existing summaries
                var response = await _httpClient.GetAsync($"{savedInfoBaseUrl}CommunitySummary_Index.json?alt=media");
                List<string> fileNames = new List<string>();

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var indexData = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonResponse);
                    fileNames = indexData["files"];
                }

                // Check if a summary for today already exists
                var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
                bool exists = fileNames.Any(fileName => fileName.Contains(today));

                if (!exists)
                {
                    // Fetch community info and claims
                    var communityInfo = await GetCommunityInfo();
                    var claims = await GetCommunityClaims();
                    int totalClaims = claims.Count(kv => kv.Value == 1);

                    // Call SaveSummaryInfo with required arguments
                    await SaveSummaryInfo(totalClaims, communityInfo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking or saving summary: {ex.Message}");
            }
        }

        public async Task SaveSummaryInfo(int totalClaims, CommunityInfo communityInfo)
        {
            try
            {
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var fileName = $"CommunitySummary_{timestamp}.json";

                var summary = new
                {
                    TotalClaims = totalClaims,
                    CommunityName = communityInfo.Name,
                    Credits = communityInfo.Credits,
                    LinkClicks = communityInfo.LinkClicks,
                    Duration = communityInfo.Duration,
                    SavedDateTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    FileName = fileName
                };

                // Save the summary to Firebase
                var json = JsonSerializer.Serialize(summary);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _httpClient.PostAsync($"{savedInfoBaseUrl}{fileName}", content);

                // Update the index file
                var indexResponse = await _httpClient.GetAsync($"{savedInfoBaseUrl}CommunitySummary_Index.json?alt=media");
                List<string> indexFiles = new List<string>();

                if (indexResponse.IsSuccessStatusCode)
                {
                    var indexJson = await indexResponse.Content.ReadAsStringAsync();
                    var indexData = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(indexJson);
                    indexFiles = indexData["files"];
                }

                // Add the new file to the index
                indexFiles.Add(fileName);
                var updatedIndex = new { files = indexFiles };
                var indexJsonContent = new StringContent(JsonSerializer.Serialize(updatedIndex), Encoding.UTF8, "application/json");

                // Save the updated index
                await _httpClient.PostAsync($"{savedInfoBaseUrl}CommunitySummary_Index.json", indexJsonContent);

                Console.WriteLine($"Summary saved successfully: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving summary: {ex.Message}");
            }
        }
    }

    public class CommunityClaim
    {
        public bool HasClaimed { get; set; } = false;
        public string ClaimDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd");
    }

    public class CommunityInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Credits { get; set; } = 0;
        public int Duration { get; set; } = 0;
        public string Code { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public int LinkClicks { get; set; } = 0;  // Ny räknare för länk-klick

        public int DaysLeft
        {
            get
            {
                if (!StartDate.HasValue) return Duration;

                var daysPassed = (DateTime.UtcNow - StartDate.Value).Days;
                var daysLeft = Math.Max(Duration - daysPassed, 0);

                if (daysLeft == 0)
                {
                    var service = new CommunityService(new HttpClient());
                    Task.Run(async () => await service.UpdateButtonVisibilityAsync("Community", false)).Wait();
                }

                return daysLeft;
            }
        }


    }
}
