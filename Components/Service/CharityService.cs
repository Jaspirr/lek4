using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;

namespace lek4.Components.Service
{
    public class CharityService
    {
        private readonly HttpClient _httpClient;
        private const string CharityFirebaseUrl = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FDailyRewards%2FCharityContributions.json";
        private const string OrganizationFirebaseUrl = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FDailyRewards%2FOrganizationContributions.json";

        public CharityService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public class ContributionData
        {
            public int ContributionCount { get; set; }
            public string LastClaimDate { get; set; }
        }

        public async Task<List<KeyValuePair<string, int>>> GetTopContributors(string contributionType, int topCount = 10)
        {
            var contributions = await GetContributions(contributionType);

            // Sorterar efter ContributionCount i fallande ordning och tar de topp "topCount" användarna
            return contributions
                .OrderByDescending(c => c.Value.ContributionCount)
                .Take(topCount)
                .Select(c => new KeyValuePair<string, int>(c.Key, c.Value.ContributionCount))
                .ToList();
        }

        public async Task<bool> CanContributeToday(string userEmail, string contributionType)
        {
            var contributions = await GetContributions(contributionType);
            if (contributions.ContainsKey(userEmail))
            {
                var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
                return contributions[userEmail].LastClaimDate != today;
            }
            return true;
        }

        public async Task<bool> Contribute(string userEmail, string contributionType)
        {
            var contributions = await GetContributions(contributionType);
            var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

            if (contributions.ContainsKey(userEmail))
            {
                if (contributions[userEmail].LastClaimDate == today)
                {
                    return false;
                }
                contributions[userEmail].ContributionCount++;
                contributions[userEmail].LastClaimDate = today;
            }
            else
            {
                contributions[userEmail] = new ContributionData
                {
                    ContributionCount = 1,
                    LastClaimDate = today
                };
            }

            return await SaveContributions(contributions, contributionType);
        }

        private async Task<Dictionary<string, ContributionData>> GetContributions(string contributionType)
        {
            string url = contributionType == "Charity" ? CharityFirebaseUrl : OrganizationFirebaseUrl;

            try
            {
                var response = await _httpClient.GetAsync($"{url}?alt=media");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<Dictionary<string, ContributionData>>(jsonResponse) ?? new Dictionary<string, ContributionData>();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await SaveContributions(new Dictionary<string, ContributionData>(), contributionType);
                    return new Dictionary<string, ContributionData>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching {contributionType} contributions: {ex.Message}");
            }

            return new Dictionary<string, ContributionData>();
        }

        private async Task<bool> SaveContributions(Dictionary<string, ContributionData> contributions, string contributionType)
        {
            string url = contributionType == "Charity" ? CharityFirebaseUrl : OrganizationFirebaseUrl;

            try
            {
                var jsonData = JsonSerializer.Serialize(contributions, new JsonSerializerOptions { WriteIndented = true });
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving {contributionType} contributions: {ex.Message}");
            }
            return false;
        }
        public async Task ResetContributions(string contributionType)
        {
            string url = contributionType == "Charity" ? CharityFirebaseUrl : OrganizationFirebaseUrl;

            try
            {
                // Fetch the current contributions
                var response = await _httpClient.GetAsync($"{url}?alt=media");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var contributions = JsonSerializer.Deserialize<Dictionary<string, ContributionData>>(jsonResponse)
                                        ?? new Dictionary<string, ContributionData>();

                    // Reset ContributionCount to 0 for all users
                    foreach (var key in contributions.Keys.ToList())
                    {
                        contributions[key].ContributionCount = 0;
                    }

                    // Save the updated contributions back to Firebase
                    var jsonData = JsonSerializer.Serialize(contributions, new JsonSerializerOptions { WriteIndented = true });
                    var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                    var saveResponse = await _httpClient.PostAsync(url, content);

                    if (!saveResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Failed to reset {contributionType} contributions.");
                    }
                }
                else
                {
                    Console.WriteLine($"No existing data found for {contributionType}, initializing new structure.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting {contributionType} contributions: {ex.Message}");
                throw;
            }
        }

    }
}
