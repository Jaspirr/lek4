using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace lek4.Components.Service
{
    public class StorageService
    {
        private readonly HttpClient _httpClient;
        private const string FirebaseBucket = "stega-426008.appspot.com";

        public StorageService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        private readonly List<string> foldersToScanAllFiles = new()
        {
            "users",
            "users/UserStats/",
             "users/SavedInfo/",
              "users/SpecialInfo/",
        };

        private readonly Dictionary<string, string[]> manualFolderFiles = new()
        {
            ["users/DailyRewards/"] = new[]
              {
                "BoostFriendClaims.json",
                "BoostFriendPairs.json",
                "BoostFriendReceived.json",
                "CharityContributions.json",
                "ClaimedDailyRewards.json",
                "CommunityClaims.json",
                "OrganizationContributions.json",
                "SpecialClaims.json"
            },
                    ["users/Jackpot/"] = new[]
                      {
                        "ClaimedPrizes.json",
                        "JackpotConfirmedTickets.json",
                        "JackpotTotalLockin.json",
                        "conversionFactor.json",
                        "outcomeconfigurations.json"
                    },
                            ["users/UserStats/"] = new[]
                            {
                                "TotalUsersOdds.json",
                                "TotalUsersWeeklyOdds.json"
                            }
            };


        public async Task<List<StorageItem>> GetStorageUsageAsync()
        {
            var usageByFolder = new List<StorageItem>();
            var maxGB = await GetMaxStorageLimitGB();

            // Hantera manuella mappar med specifika filer
            foreach (var kvp in manualFolderFiles)
            {
                string folder = kvp.Key;
                var files = kvp.Value;
                long totalBytes = 0;

                foreach (var file in files)
                {
                    string encodedPath = Uri.EscapeDataString(folder + file);
                    string url = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/{encodedPath}?alt=media";

                    try
                    {
                        using var request = new HttpRequestMessage(HttpMethod.Get, url);
                        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                        if (response.IsSuccessStatusCode &&
                            response.Content.Headers.ContentLength.HasValue)
                        {
                            totalBytes += response.Content.Headers.ContentLength.Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error getting size for {folder}{file}: {ex.Message}");
                    }
                }

                var sizeGB = totalBytes / 1_073_741_824.0;
                usageByFolder.Add(new StorageItem
                {
                    Folder = folder,
                    SizeGB = Math.Round(sizeGB, 6),
                    PercentUsed = (maxGB > 0) ? (sizeGB / maxGB) * 100 : 0
                });
            }

            // Hantera dynamiska mappar – skanna allt innehåll
            foreach (var folder in foldersToScanAllFiles)
            {
                long folderBytes = 0;
                string listUrl = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o?prefix={Uri.EscapeDataString(folder)}&alt=json";

                try
                {
                    var response = await _httpClient.GetAsync(listUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);

                        if (doc.RootElement.TryGetProperty("items", out var items))
                        {
                            foreach (var item in items.EnumerateArray())
                            {
                                if (item.TryGetProperty("name", out var nameElement))
                                {
                                    var filePath = nameElement.GetString();
                                    var fileUrl = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/{Uri.EscapeDataString(filePath)}?alt=media";

                                    try
                                    {
                                        using var headReq = new HttpRequestMessage(HttpMethod.Head, fileUrl);
                                        var headRes = await _httpClient.SendAsync(headReq);

                                        if (headRes.IsSuccessStatusCode &&
                                            headRes.Content.Headers.ContentLength.HasValue)
                                        {
                                            folderBytes += headRes.Content.Headers.ContentLength.Value;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"HEAD error for {filePath}: {ex.Message}");
                                    }
                                }
                            }
                        }

                        usageByFolder.Add(new StorageItem
                        {
                            Folder = folder,
                            SizeGB = Math.Round(folderBytes / 1_073_741_824.0, 6),
                            PercentUsed = Math.Round((folderBytes / 1_073_741_824.0) / maxGB * 100, 2)
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Failed to list files in folder {folder}: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error scanning folder {folder}: {ex.Message}");
                }
            }
            return usageByFolder;
        }

            private string ExtractSubfolder(string fullPath)
        {
            var parts = fullPath.Split('/');
            if (parts.Length >= 3)
            {
                return $"{parts[0]}/{parts[1]}/"; // "users/UserStats/"
            }
            return parts[0] + "/"; // fallback: "users/"
        }


        private async Task<long> GetTotalSizeForFolder(string folder)
        {
            long totalSize = 0;

            try
            {
                string firebaseListUrl = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o?prefix=users/";
                var response = await _httpClient.GetAsync(firebaseListUrl);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.TryGetProperty("items", out var items))
                    {
                        foreach (var item in items.EnumerateArray())
                        {
                            if (item.TryGetProperty("size", out var sizeElement) &&
                                long.TryParse(sizeElement.GetString(), out var size))
                            {
                                totalSize += size;
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to list objects in {folder}. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing objects in {folder}: {ex.Message}");
            }

            return totalSize;
        }

        public async Task<double> GetMaxStorageLimitGB()
        {
            try
            {
                var url = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fconfig%2FStorageMaxLimit.json?alt=media";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var config = JsonSerializer.Deserialize<Dictionary<string, double>>(json);
                    if (config != null && config.TryGetValue("maxStorageGB", out double limit))
                        return limit;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine("StorageMaxLimit.json not found. Uploading default...");
                    await SaveMaxStorageLimit(5.0);
                    return 5.0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching maxStorageLimit: {ex.Message}");
            }

            return 5.0; // fallback
        }

        public async Task<bool> SaveMaxStorageLimit(double maxGB)
        {
            try
            {
                var url = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fconfig%2FStorageMaxLimit.json";
                var json = JsonSerializer.Serialize(new { maxStorageGB = maxGB });
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving max storage limit: {ex.Message}");
                return false;
            }
        }

        public class StorageItem
        {
            public string Folder { get; set; }
            public double SizeGB { get; set; }
            public double PercentUsed { get; set; }
        }
    }
}
