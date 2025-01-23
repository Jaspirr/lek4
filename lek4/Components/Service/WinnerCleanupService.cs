using System;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;

public class WinnerCleanupService
{
    private readonly HttpClient _httpClient;

    private const string WinnerFilesBaseUrl = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fwinner%2F";
    private const string CleanupConfigUrl = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fconfig%2FCleanupConfig.json";
    private const string SavedInfoBaseUrl = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FSavedInfo%2F";

    public WinnerCleanupService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task CheckAndCleanupWinnerFilesAsync()
    {
        try
        {
            var cleanupConfig = await GetCleanupConfigAsync();
            if (cleanupConfig == null || string.IsNullOrEmpty(cleanupConfig.DayOfWeek))
            {
                Console.WriteLine("No cleanup day configured. Skipping cleanup.");
                return;
            }

            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var currentDay = DateTime.UtcNow.DayOfWeek.ToString();

            if (!string.Equals(currentDay, cleanupConfig.DayOfWeek, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Today is {currentDay}. Cleanup scheduled for {cleanupConfig.DayOfWeek}. Skipping cleanup.");
                return;
            }

            if (cleanupConfig.LastResetDay == today)
            {
                Console.WriteLine("Cleanup has already been performed today. Skipping cleanup.");
                return;
            }

            Console.WriteLine("Starting backup and cleanup process...");

            cleanupConfig.LastResetDay = today;
            await UpdateCleanupConfigAsync(cleanupConfig);

            await BackupWinnerFilesAsync();
            await CleanupWinnerFilesAsync();

            Console.WriteLine("Backup and cleanup process completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during cleanup process: {ex.Message}");
        }
    }

    private async Task<CleanupConfig> GetCleanupConfigAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{CleanupConfigUrl}?alt=media");
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CleanupConfig>(jsonResponse);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching cleanup config: {ex.Message}");
        }
        return null;
    }

    private async Task BackupWinnerFilesAsync()
    {
        var updatedIndex = await GetExistingIndex(); // Hämta nuvarande indexfil

        try
        {
            for (int i = 1; i <= 10; i++) // Iterate through product folders
            {
                var winnerFileUrl = $"{WinnerFilesBaseUrl}product{i}%2Fwinner.json?alt=media";
                var response = await _httpClient.GetAsync(winnerFileUrl);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    if (jsonResponse.Contains("\"WinningTicket\""))
                    {
                        // Hantera jackpotvinst
                        var jackpotWinner = JsonSerializer.Deserialize<JackpotWinnerInfo>(jsonResponse);
                        if (jackpotWinner != null)
                        {
                            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                            jackpotWinner.FileName = $"Jackpot_Product{i}_{timestamp}.json";

                            var content = new StringContent(JsonSerializer.Serialize(jackpotWinner), Encoding.UTF8, "application/json");
                            var saveResponse = await _httpClient.PostAsync($"{SavedInfoBaseUrl}{jackpotWinner.FileName}", content);

                            if (saveResponse.IsSuccessStatusCode)
                            {
                                Console.WriteLine($"Backup successful for {jackpotWinner.FileName}");
                                if (!updatedIndex.Contains(jackpotWinner.FileName))
                                    updatedIndex.Add(jackpotWinner.FileName);
                            }
                            else
                            {
                                Console.WriteLine($"Failed to backup {jackpotWinner.FileName}");
                            }
                        }
                    }
                    else
                    {
                        // Hantera produktvinst
                        var productWinner = JsonSerializer.Deserialize<ProductWinnerInfo>(jsonResponse);
                        if (productWinner != null)
                        {
                            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                            productWinner.FileName = $"Product_Product{i}_{timestamp}.json";

                            var content = new StringContent(JsonSerializer.Serialize(productWinner), Encoding.UTF8, "application/json");
                            var saveResponse = await _httpClient.PostAsync($"{SavedInfoBaseUrl}{productWinner.FileName}", content);

                            if (saveResponse.IsSuccessStatusCode)
                            {
                                Console.WriteLine($"Backup successful for {productWinner.FileName}");
                                if (!updatedIndex.Contains(productWinner.FileName))
                                    updatedIndex.Add(productWinner.FileName);
                            }
                            else
                            {
                                Console.WriteLine($"Failed to backup {productWinner.FileName}");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"No winner.json found for product{i}. Skipping...");
                }
            }

            // Uppdatera indexfilen efter att säkerhetskopieringen är klar
            await UpdateIndexFile(updatedIndex);

            Console.WriteLine("Index file updated successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during backup of winner files: {ex.Message}");
        }
    }
    private async Task UpdateIndexFile(List<string> updatedIndex)
    {
        try
        {
            var content = new StringContent(JsonSerializer.Serialize(updatedIndex), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{SavedInfoBaseUrl}SavedInfo_Index.json", content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Index file updated successfully.");
            }
            else
            {
                Console.WriteLine("Failed to update index file.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating index file: {ex.Message}");
        }
    }

    private async Task<List<string>> GetExistingIndex()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{SavedInfoBaseUrl}SavedInfo_Index.json?alt=media");
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<string>>(jsonResponse) ?? new List<string>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching existing index: {ex.Message}");
        }
        return new List<string>();
    }

    private async Task CleanupWinnerFilesAsync()
    {
        try
        {
            for (int i = 1; i <= 10; i++) // Iterate through product folders
            {
                var winnerFileUrl = $"{WinnerFilesBaseUrl}product{i}%2Fwinner.json";
                var deleteResponse = await _httpClient.DeleteAsync(winnerFileUrl);

                if (deleteResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Deleted product{i}/winner.json");
                }
                else
                {
                    Console.WriteLine($"Failed to delete product{i}/winner.json");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during cleanup of winner files: {ex.Message}");
        }
    }

    private async Task UpdateCleanupConfigAsync(CleanupConfig config)
    {
        try
        {
            var jsonContent = new StringContent(JsonSerializer.Serialize(config), Encoding.UTF8, "application/json");
            await _httpClient.PostAsync(CleanupConfigUrl, jsonContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating cleanup config: {ex.Message}");
        }
    }

    public class JackpotWinnerInfo
    {
        public string Winner { get; set; }
        public string DrawDate { get; set; }
        public int JackpotAmount { get; set; }
        public WinningTicketInfo WinningTicket { get; set; } = new WinningTicketInfo();
        public string FileName { get; set; }

        public class WinningTicketInfo
        {
            public string UserEmail { get; set; }
            public string Number { get; set; }
            public string Color { get; set; }
            public string Symbol { get; set; }
            public string ChineseSymbol { get; set; }
            public string Planet { get; set; }
            public string Element { get; set; }
        }
    }
    public class ProductWinnerInfo
    {
        public string Winner { get; set; }
        public string Timestamp { get; set; }
        public int Price { get; set; }
        public string FileName { get; set; }
    }

    private class CleanupConfig
    {
        public string DayOfWeek { get; set; }
        public string LastResetDay { get; set; }
    }
}
