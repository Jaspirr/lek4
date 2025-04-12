using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;

namespace lek4.Components.Service
{
    public class InfoConfigService
    {
        private readonly HttpClient _httpClient;
        private const string FirebaseUrl = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fconfig%2FInfoConfig.json";

        public InfoConfigService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Hämtar den aktuella informationen från Firebase.
        /// Om filen inte finns, skapas den med standardvärden.
        /// </summary>
        public async Task<InfoConfig> GetInfoConfig()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{FirebaseUrl}?alt=media");

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var infoConfig = JsonSerializer.Deserialize<InfoConfig>(jsonResponse);
                    return infoConfig ?? new InfoConfig();
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine("InfoConfig.json not found. Creating a new file with default values...");
                    var defaultConfig = new InfoConfig();
                    await UpdateInfoConfig(defaultConfig);
                    return defaultConfig;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching info config: {ex.Message}");
            }

            return new InfoConfig();
        }

        /// <summary>
        /// Uppdaterar informationen i Firebase med nya värden.
        /// Om filen inte finns, skapas den.
        /// </summary>
        public async Task<bool> UpdateInfoConfig(InfoConfig infoConfig)
        {
            try
            {
                var jsonData = JsonSerializer.Serialize(infoConfig, new JsonSerializerOptions { WriteIndented = true });
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(FirebaseUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("InfoConfig updated successfully.");
                    return true;
                }

                Console.WriteLine($"Failed to update InfoConfig. Status code: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating info config: {ex.Message}");
                return false;
            }
        }
    }

    public class CharityInfo
    {
        public string CharityName { get; set; } = "Default Charity";
        public string Description { get; set; } = "Default description.";
    }

    public class OrganizationInfo
    {
        public string OrganizationName { get; set; } = "Default Organization";
        public string Mission { get; set; } = "Default mission.";
        public string Focus { get; set; } = "Default focus.";
    }

    public class InfoConfig
    {
        public CharityInfo Charity { get; set; } = new CharityInfo();
        public OrganizationInfo Organization { get; set; } = new OrganizationInfo();
    }
}
