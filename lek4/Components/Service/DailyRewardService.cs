using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;

namespace lek4.Components.Service
{
    public class DailyRewardService
    {
        private readonly HttpClient _httpClient;
        private readonly UserService _userService;
        private const string DailyRewardUrl = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FDailyRewards%2FClaimedDailyRewards.json";

        public DailyRewardService(HttpClient httpClient, UserService userService)
        {
            _httpClient = httpClient;
            _userService = userService;
        }

        /// <summary>
        /// Kontrollera om användaren har tagit dagens belöning baserat på dagens datum.
        /// </summary>
        public async Task<bool> HasClaimedDailyReward(string userEmail)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{DailyRewardUrl}?alt=media");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Skapa en tom fil om den inte finns
                    await CreateEmptyDailyRewardsFile();
                    return false;
                }

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var rewardData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(jsonResponse);

                if (rewardData != null && rewardData.ContainsKey(userEmail))
                {
                    if (rewardData[userEmail].ContainsKey("LastClaimDate"))
                    {
                        var lastClaimDate = DateTime.Parse(rewardData[userEmail]["LastClaimDate"]);
                        return lastClaimDate.Date == DateTime.UtcNow.Date;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Lägger till 1 credit och markerar belöningen som hämtad, med dagens datum.
        /// </summary>
        public async Task<bool> ClaimDailyReward(string userEmail)
        {
            try
            {
                // Kontrollera om användaren redan har tagit belöningen idag
                if (await HasClaimedDailyReward(userEmail))
                {
                    return false;
                }

                // Hämta nuvarande data från Firebase
                var response = await _httpClient.GetAsync($"{DailyRewardUrl}?alt=media");
                var rewardData = new Dictionary<string, Dictionary<string, string>>();

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    rewardData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(jsonResponse)
                                 ?? new Dictionary<string, Dictionary<string, string>>();
                }

                // Lägg till eller uppdatera användaren med dagens datum
                rewardData[userEmail] = new Dictionary<string, string>
                {
                    { "DailyRewards", "true" },
                    { "LastClaimDate", DateTime.UtcNow.ToString("yyyy-MM-dd") }
                };

                // Uppdatera användarens credits och total credits
                await _userService.AddCreditToUser(userEmail, 1);
                await _userService.AddToTotalCredits(1);

                // Serialisera och spara tillbaka till Firebase
                var updatedJson = JsonSerializer.Serialize(rewardData);
                var content = new StringContent(updatedJson, Encoding.UTF8, "application/json");

                var uploadResponse = await _httpClient.PostAsync(DailyRewardUrl, content);
                return uploadResponse.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error claiming daily reward: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Skapar en tom JSON-fil om DailyRewards-filen inte existerar.
        /// </summary>
        private async Task CreateEmptyDailyRewardsFile()
        {
            try
            {
                var emptyData = new Dictionary<string, Dictionary<string, string>>();
                var emptyJson = JsonSerializer.Serialize(emptyData);
                var content = new StringContent(emptyJson, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(DailyRewardUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Failed to create the empty ClaimedDailyRewards.json file.");
                }
                else
                {
                    Console.WriteLine("Successfully created an empty ClaimedDailyRewards.json file.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating empty rewards file: {ex.Message}");
            }
        }
    }
}
