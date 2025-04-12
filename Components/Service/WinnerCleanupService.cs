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
    private const string TicketsUrl = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FJackpot%2FJackpotConfirmedTickets.json?alt=media";
    private const string IndexFileUrl = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FSavedInfo%2FSavedTicketsIndex.json";

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

                                // 🔹 Lägg till DrawDate-uppdatering här
                                await UpdateJackpotDrawDateAsync(i);
                                await SaveAndClearJackpotTicketsAsync();

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
    private async Task SaveAndClearJackpotTicketsAsync()
    {
        try
        {
            Console.WriteLine("🔹 Fetching jackpot tickets...");

            var response = await _httpClient.GetAsync(TicketsUrl);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("❌ No tickets found to save.");
                return;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(jsonResponse) || jsonResponse == "{}")
            {
                Console.WriteLine("❌ No tickets available.");
                return;
            }

            var ticketsData = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonResponse);
            if (ticketsData == null || ticketsData.Count == 0)
            {
                Console.WriteLine("❌ No tickets available to save.");
                return;
            }

            int userCount = ticketsData.Count;
            int ticketCount = ticketsData.Values.Sum(t => t.Count);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var fileName = $"JackpotTickets_{timestamp}.json";

            var savedTicketFile = new SavedTicketFile
            {
                FileName = fileName,
                SavedDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                UserCount = userCount,
                TicketCount = ticketCount,
                Tickets = ticketsData
            };

            var jsonContent = JsonSerializer.Serialize(savedTicketFile, new JsonSerializerOptions { WriteIndented = true });

            Console.WriteLine($"🔍 JSON Payload: {jsonContent}");

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var saveResponse = await _httpClient.PostAsync($"{SavedInfoBaseUrl}{fileName}?alt=media", content);

            if (saveResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"✅ Tickets saved as {fileName}");
                var indexResponse = await _httpClient.GetAsync(IndexFileUrl);
                List<string> fileIndex = new();

                if (indexResponse.IsSuccessStatusCode)
                {
                    var indexJson = await indexResponse.Content.ReadAsStringAsync();

                    try
                    {
                        if (!string.IsNullOrWhiteSpace(indexJson) && indexJson.Trim() != "null")
                        {
                            fileIndex = JsonSerializer.Deserialize<List<string>>(indexJson) ?? new List<string>();
                        }
                        else
                        {
                            fileIndex = new List<string>(); // Skapa en ny tom lista om indexfilen är tom eller null
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"⚠ JSON parsing error in index file: {ex.Message}");
                        fileIndex = new List<string>(); // Återställ till en tom lista om JSON är korrupt
                    }
                }
                if (!fileIndex.Contains(fileName))
                {
                    fileIndex.Add(fileName);
                }

                await UpdateticketIndexFile(fileIndex);

                var clearContent = new StringContent("{}", Encoding.UTF8, "application/json");
                await _httpClient.PostAsync(TicketsUrl, clearContent);

                Console.WriteLine($"✅ Jackpot tickets cleared after saving!");
            }
            else
            {
                Console.WriteLine("❌ Failed to save tickets.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error saving jackpot tickets: {ex.Message}");
        }
    }
    private async Task UpdateticketIndexFile(List<string> newEntries)
    {
        try
        {
            Console.WriteLine("🔹 Fetching current index file...");

            // 🔹 Hämta den nuvarande indexfilen från Firebase
            var indexResponse = await _httpClient.GetAsync($"{IndexFileUrl}?alt=media");
            List<string> fileIndex = new();

            if (indexResponse.IsSuccessStatusCode)
            {
                var indexJson = await indexResponse.Content.ReadAsStringAsync();

                try
                {
                    if (!string.IsNullOrWhiteSpace(indexJson) && indexJson.Trim() != "null")
                    {
                        fileIndex = JsonSerializer.Deserialize<List<string>>(indexJson) ?? new List<string>();
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"⚠ JSON parsing error in index file: {ex.Message}");
                    fileIndex = new List<string>(); // Återställ till en tom lista om JSON är korrupt
                }
            }
            else
            {
                Console.WriteLine("⚠ No existing index file found. Creating a new one.");
                fileIndex = new List<string>(); // Om filen inte finns, skapa en ny lista
            }

            // 🔹 Lägg till nya poster om de inte redan finns
            bool updated = false;
            foreach (var entry in newEntries)
            {
                if (!fileIndex.Contains(entry))
                {
                    fileIndex.Add(entry);
                    updated = true;
                }
            }

            // 🔹 Om inget nytt har lagts till, avbryt för att undvika onödig skrivning
            if (!updated)
            {
                Console.WriteLine("✅ Index file is already up-to-date. No changes needed.");
                return;
            }

            // 🔹 Skriv tillbaka den uppdaterade listan till Firebase
            var jsonContent = JsonSerializer.Serialize(fileIndex, new JsonSerializerOptions { WriteIndented = true });

            Console.WriteLine($"🔍 Uppdaterad Index-fil JSON: {jsonContent}");

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // 🔹 Vi testar att använda `POST` istället för `PUT` för att skriva till Firebase
            var response = await _httpClient.PostAsync($"{IndexFileUrl}?alt=media", content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ Index file updated successfully.");
            }
            else
            {
                Console.WriteLine($"❌ Failed to update index file. Status: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error updating index file: {ex.Message}");
        }
    }

    public class SavedTicketFile
    {
        public string FileName { get; set; }
        public string SavedDate { get; set; }
        public int UserCount { get; set; }
        public int TicketCount { get; set; }
        public Dictionary<string, List<string>> Tickets { get; set; } = new();
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
    private async Task UpdateJackpotDrawDateAsync(int productNumber)
    {
        var productInfoUrl = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2FproductInfo.json?alt=media";
        var updateUrl = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2FproductInfo.json";
        var configUrl = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fconfig%2FJackpotDrawConfig.json?alt=media";

        try
        {
            // 🔹 Hämta antal dagar att lägga till från konfigurationsfilen
            int daysToAdd = 7; // Standardvärde
            var configResponse = await _httpClient.GetAsync(configUrl);
            if (configResponse.IsSuccessStatusCode)
            {
                var configJson = await configResponse.Content.ReadAsStringAsync();
                var config = JsonSerializer.Deserialize<JackpotDrawConfig>(configJson);
                if (config != null)
                {
                    daysToAdd = config.DaysToAdd;
                }
            }

            // 🔹 Hämta befintlig productInfo.json från Firebase
            var response = await _httpClient.GetAsync(productInfoUrl);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"No productInfo.json found for product {productNumber}. Skipping draw date update.");
                return;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var productInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse);

            if (productInfo == null || !productInfo.ContainsKey("DrawDate"))
            {
                Console.WriteLine($"Invalid productInfo.json format for product {productNumber}. Skipping.");
                return;
            }

            // 🔹 Uppdatera ENDAST DrawDate genom att lägga till konfigurerade dagar
            DateTime currentDrawDate = DateTime.Parse(productInfo["DrawDate"].ToString()).ToUniversalTime();
            DateTime newDrawDate = currentDrawDate.AddDays(daysToAdd);

            productInfo["DrawDate"] = newDrawDate.ToString("yyyy-MM-ddTHH:mm:ss");

            // 🔹 Uppdatera endast DrawDate i Firebase utan att radera övrig data
            var updatedJson = JsonSerializer.Serialize(productInfo, new JsonSerializerOptions { WriteIndented = true });
            var content = new StringContent(updatedJson, Encoding.UTF8, "application/json");
            var updateResponse = await _httpClient.PostAsync(updateUrl, content);

            if (updateResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"✅ Successfully updated DrawDate for product {productNumber} to {newDrawDate}");
            }
            else
            {
                Console.WriteLine($"❌ Failed to update DrawDate for product {productNumber}. Status code: {updateResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error updating DrawDate for product {productNumber}: {ex.Message}");
        }
    }

    private class ProductInfo
    {
        public DateTime DrawDate { get; set; }
    }


    private class JackpotDrawConfig
    {
        public int DaysToAdd { get; set; }
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
