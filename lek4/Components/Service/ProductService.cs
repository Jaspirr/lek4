using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using Blazored.LocalStorage;
using System.Net;

namespace lek4.Components.Service
{
    public class ProductService
    {
        private readonly HttpClient _httpClient;
        private readonly StatsService _statsService;
        private readonly UserService _userService;
        private readonly ILocalStorageService _localStorage;
        private Dictionary<int, DateTime> productEndTimes = new Dictionary<int, DateTime>();


        public ProductService(HttpClient httpClient, UserService userService, ILocalStorageService localStorage, StatsService statsService)
        {
            _httpClient = httpClient;
            _userService = userService;
            _localStorage = localStorage;
            _statsService = statsService; 
        }

        // Add a new product to Firebase with productInfo.json
        public async Task AddProductToFirebase(int productNumber, double price, string userEmail, string productName, string imageUrl, bool isJackpot)
        {
            // Define the lock-in amount
            double lockInAmount = 0.0; // Initial lock-in amount

            // Call SaveProductData with all the required arguments, including the new `isJackpot` parameter
            await SaveProductData(productNumber, userEmail, lockInAmount, price, productName, imageUrl, isJackpot);
        }
        public async Task AddJackpotToFirebase(string productName, string imageUrl, double startingPrizePool)
        {
            // Skapa en unik productNumber för jackpot, t.ex. 999
            int productNumber = 999;

            // Definiera och spara jackpot som en produkt
            await SaveProductData(
                productNumber,
                userEmail: null,    // Ingen specifik användare vid skapande av jackpot
                lockInAmount: 0,
                price: 100,         // Pris för att delta
                productName: productName,
                imageUrl: imageUrl,
                isJackpot: true     // Markera som jackpot
            );

            // Initialisera prispotten
            await UpdatePrizePool(productNumber, startingPrizePool);
        }



        public async Task SaveUserToProduct(int productNumber, int userId, ProductData productData)
        {
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/products/product{productNumber}/user{userId}.json";
            var userJson = JsonSerializer.Serialize(productData);
            var content = new StringContent(userJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(path, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"User {productData.UserEmail} saved successfully for product {productNumber}.");
            }
            else
            {
                Console.WriteLine($"Failed to save user {productData.UserEmail} for product {productNumber}. Error: {response.StatusCode}");
            }
        }


        // Save product info to Firebase
        public async Task SaveProductData(int productNumber, string userEmail, double lockInAmount, double price, string productName, string imageUrl, bool isJackpot)
        {
            var productData = new ProductData
            {
                ProductNumber = productNumber,
                UserEmail = userEmail,
                LockInAmount = lockInAmount,
                Price = price,
                ProductName = productName,
                ImageUrl = imageUrl,
                IsJackpot = isJackpot // Set jackpot status
            };

            var productJson = JsonSerializer.Serialize(productData);
            Console.WriteLine($"Serialized JSON: {productJson}");

            // Firebase Storage URL (without ?alt=media for uploading)
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o?name=users/products/product{productNumber}/productInfo.json";

            var content = new StringContent(productJson, Encoding.UTF8, "application/json");

            // Use POST to upload the product data
            var response = await _httpClient.PostAsync(path, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response content: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Product data for {productNumber} saved successfully.");
            }
            else
            {
                Console.WriteLine($"Failed to save product data for {productNumber}. Error: {response.StatusCode}");
            }
        }
     
        public async Task<string> GetWinnerFromFirebase(int productNumber)
        {
            // Firebase URL to fetch winner data for a product
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fwinner%2Fproduct{productNumber}%2Fwinner.json?alt=media";

            // Send a GET request to Firebase
            var response = await _httpClient.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                // Deserialize the response content
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var winnerData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse);

                // Extract and return the winner email
                if (winnerData.ContainsKey("Winner"))
                {
                    return winnerData["Winner"].ToString();
                }
                else
                {
                    Console.WriteLine($"No winner data found for product {productNumber}");
                    return null;
                }
            }
            else
            {
                Console.WriteLine($"Failed to fetch winner for product {productNumber}. Status code: {response.StatusCode}");
                return null;
            }
        }

        // Fetch all products from Firebase
        public async Task<List<ProductData>> FetchAllProductsFromFirebaseAsync()
        {
            var allProducts = new List<ProductData>();
            int productNumber = 1;

            while (true)
            {
                var product = await GetProductFromFirebaseAsync(productNumber);
                if (product != null)
                {
                    allProducts.Add(product);
                    productNumber++;
                }
                else
                {
                    break; // Stop fetching when no more products are found
                }
            }

            return allProducts;
        }

        // Get product details from Firebase for a specific product number
        public async Task<ProductData> GetProductFromFirebaseAsync(int productNumber)
        {
            try
            {
                var url = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2FproductInfo.json?alt=media";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var product = JsonSerializer.Deserialize<ProductData>(jsonResponse);

                    return product;
                }
                else
                {
                    Console.WriteLine($"Failed to fetch product {productNumber}. Status code: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while fetching product {productNumber}: {ex.Message}");
                return null;
            }
        }
        public async Task RemoveProductFromFirebase(int productNumber)
        {
            // Define the path to the product file
            var productInfoPath = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users/products/product{productNumber}/productInfo.json";
            // JSON content marking the product as removed
            var removedContent = new StringContent("{\"removed\": true}", Encoding.UTF8, "application/json");

            try
            {
                // Send PATCH request to update the file with removed metadata/content
                var response = await _httpClient.PatchAsync(productInfoPath, removedContent);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Product {productNumber} marked as removed in Firebase.");
                }
                else
                {
                    Console.WriteLine($"Failed to mark product {productNumber} as removed. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred while trying to mark product {productNumber} as removed: {ex.Message}");
            }
        }

        // Lock in a user for a product
        public async Task LockInUser(int productNumber, string userEmail, double lockInAmount)
        {
            if (string.IsNullOrEmpty(userEmail) || userEmail == "Anonymous")
            {
                Console.WriteLine("Invalid email. Cannot lock in as Anonymous.");
                return;
            }

            // Hämta användarens befintliga lock-in-data från Firebase (om det finns)
            var existingUser = await GetUserFromFirebase(productNumber, userEmail);
            bool isNewUser = existingUser == null;

            if (existingUser != null)
            {
                // Om användaren redan har ett lock-in belopp, addera det nya beloppet
                existingUser.LockInAmount += lockInAmount;
                lockInAmount = existingUser.LockInAmount;  // Uppdatera beloppet som ska sparas
                Console.WriteLine($"Updated lock-in amount for {userEmail} on product {productNumber}");
            }

            // Skapa eller uppdatera användardata
            var userLockInData = new
            {
                UserEmail = userEmail,
                LockInAmount = lockInAmount
            };

            // Serialisera användarens data till JSON
            var userJson = JsonSerializer.Serialize(userLockInData);
            var content = new StringContent(userJson, Encoding.UTF8, "application/json");

            // Ange Firebase-sökvägen för att spara användaren baserat på e-post
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2F{userEmail}.json";

            // Använd POST för att spara data till Firebase
            var response = await _httpClient.PostAsync(path, content);

            var responseContent = await response.Content.ReadAsStringAsync();  // Logga responsen för felsökning
            Console.WriteLine($"Response content: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Lock-in for {userEmail} saved successfully for product {productNumber}.");

                // Uppdatera totalusers.json med användarens lock-in amount
                await UpdateTotalUsers(productNumber, userEmail);

                // Uppdatera totalstats.json med det nya beloppet
                await _statsService.UpdateTotalStats(productNumber, userEmail, lockInAmount);

            }
            else
            {
                Console.WriteLine($"Failed to save lock-in for {userEmail} on product {productNumber}. Error: {response.StatusCode}");
            }
        }

        public async Task<int> GetNextUserNumber(int productNumber)
        {
            // Hämta alla användare för produkten och räkna antalet användare
            var existingUsers = await GetLockedInUsersFromFirebase(productNumber);

            // Returnera nästa lediga användar-ID
            return existingUsers.Count + 1;
        }


        private async Task<ProductData> GetUserFromFirebase(int productNumber, string userEmail)
        {
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2F{userEmail}.json?alt=media";
            var response = await _httpClient.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                try
                {
                    // Deserialisera JSON till ett ProductData-objekt
                    var user = JsonSerializer.Deserialize<ProductData>(jsonResponse);
                    return user;
                }
                catch (JsonException)
                {
                    Console.WriteLine("Failed to deserialize user data.");
                    return null;
                }
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Om användaren inte finns, returnera null
                return null;
            }
            else
            {
                Console.WriteLine($"Failed to fetch user data for {userEmail}. Status code: {response.StatusCode}");
                return null;
            }
        }

        public async Task<int> GetLockInCount(int productNumber)
        {
            try
            {
                // Firebase path för att hämta lock-in count
                var url = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users/products/product{productNumber}/lockInCount.json?alt=media";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    // Deserialisera svaret till ett heltal (lock-in count)
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<int>(jsonResponse);
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    // Om lock-in count-filen inte existerar, returnera 0
                    return 0;
                }
                else
                {
                    Console.WriteLine($"Failed to fetch lock-in count for product {productNumber}. Status code: {response.StatusCode}");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching lock-in count: {ex.Message}");
                return 0;
            }
        }
        // Fetch locked-in users from Firebase
        private async Task<List<ProductData>> GetLockedInUsersFromFirebase(int productNumber)
        {
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2FlockedInUsers.json?alt=media";
            var response = await _httpClient.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();

                try
                {
                    // Försök att deserialisera JSON som en lista av ProductData
                    var lockedInUsers = JsonSerializer.Deserialize<List<ProductData>>(jsonResponse);
                    return lockedInUsers ?? new List<ProductData>(); // Returnera tom lista om null
                }
                catch (JsonException)
                {
                    Console.WriteLine("Failed to deserialize JSON into List<ProductData>. Make sure the JSON is an array.");
                    return new List<ProductData>(); // Returnera en tom lista vid fel
                }
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Ingen användare hittades, returnera en tom lista
                return new List<ProductData>();
            }
            else
            {
                Console.WriteLine($"Failed to fetch users for product {productNumber}. Status code: {response.StatusCode}");
                return new List<ProductData>();
            }
        }

        // Save the list of locked-in users to Firebase
        private async Task SaveLockedInUsersToFirebase(int productNumber, List<ProductData> users)
        {
            // Serialisera användarlistan
            var lockedInUsersData = JsonSerializer.Serialize(users);
            var content = new StringContent(lockedInUsersData, Encoding.UTF8, "application/json");

            // Spara den uppdaterade listan av användare till Firebase
            var response = await _httpClient.PutAsync($"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2FlockedInUsers.json", content);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to save locked-in users for product {productNumber}. Error: {response.StatusCode}");
            }
        }
        public async Task<double> GetPriceForProduct(int productNumber)
        {
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2FproductInfo.json?alt=media";
            var response = await _httpClient.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var productInfo = JsonSerializer.Deserialize<ProductData>(jsonResponse);

                return productInfo.Price;  // Assuming the Price field exists in productInfo.json
            }
            else
            {
                Console.WriteLine($"Failed to fetch product info for product {productNumber}. Status code: {response.StatusCode}");
                return 0.0;  // Default to 0 if the price cannot be fetched
            }
        }
        public async Task JoinJackpot(int productNumber)
        {
            // Kontrollera att `CurrentUserEmail` är satt
            if (string.IsNullOrEmpty(_userService.CurrentUserEmail))
            {
                Console.WriteLine("Current user email is not set. Cannot proceed with JoinJackpot.");
                return;
            }

            // Hämta användarens krediter med `GetUserCredits`
            double userCredits = await _userService.GetUserCredits();
            if (userCredits < 100)
            {
                Console.WriteLine("User does not have enough credits to join the jackpot.");
                return;
            }

            // Dra av 100 credits och uppdatera användarens saldo
            userCredits -= 100;
            await _userService.UpdateUserCredits(_userService.CurrentUserEmail, userCredits);

            // Uppdatera prispotten och deltagarlista för jackpotten
            double currentPrizePool = await GetPrizePool(productNumber);
            currentPrizePool += 10; // Lägg till 10 credits i prispotten
            await UpdatePrizePool(productNumber, currentPrizePool);

            // Lägg till användaren som deltagare
            var participants = await GetJackpotParticipants(productNumber);
            if (!participants.Contains(_userService.CurrentUserEmail))
            {
                participants.Add(_userService.CurrentUserEmail);
                await SaveParticipants(productNumber, participants);
            }

            Console.WriteLine($"{_userService.CurrentUserEmail} joined the jackpot for product {productNumber}.");
        }
        private async Task<List<string>> GetJackpotParticipants(int productNumber)
        {
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2Fparticipants.json?alt=media";
            var response = await _httpClient.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<string>>(jsonResponse) ?? new List<string>();
            }
            return new List<string>();
        }

        private async Task SaveParticipants(int productNumber, List<string> participants)
        {
            var participantsJson = JsonSerializer.Serialize(participants);
            var content = new StringContent(participantsJson, Encoding.UTF8, "application/json");

            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2Fparticipants.json";
            await _httpClient.PutAsync(path, content);
        }

        private async Task<double> GetPrizePool(int productNumber)
        {
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2FprizePool.json?alt=media";
            var response = await _httpClient.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<double>(jsonResponse);
            }
            return 0.0;
        }

        private async Task UpdatePrizePool(int productNumber, double prizePool)
        {
            var prizeJson = JsonSerializer.Serialize(prizePool);
            var content = new StringContent(prizeJson, Encoding.UTF8, "application/json");

            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2FprizePool.json";
            await _httpClient.PutAsync(path, content);
        }

        public async Task<List<ProductData>> GetUsersForProduct(int productNumber)
        {
            // Firebase path to the locked-in users file (e.g., lockedInUsers.json)
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users/products/product{productNumber}/lockedInUsers.json?alt=media";

            // Send an HTTP GET request to fetch locked-in users for the product
            var response = await _httpClient.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                try
                {
                    // Deserialize the JSON response into a list of ProductData
                    var users = JsonSerializer.Deserialize<List<ProductData>>(jsonResponse);
                    return users ?? new List<ProductData>(); // Return empty list if deserialization fails
                }
                catch (JsonException)
                {
                    Console.WriteLine("Failed to deserialize JSON into List<ProductData>. Make sure the JSON is an array.");
                    return new List<ProductData>();
                }
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // No users found, return an empty list
                return new List<ProductData>();
            }
            else
            {
                Console.WriteLine($"Failed to fetch users for product {productNumber}. Status code: {response.StatusCode}");
                return new List<ProductData>();
            }
        }
        public TimeSpan GetTimeRemaining(int productNumber)
        {
            if (productEndTimes.ContainsKey(productNumber))
            {
                var endTime = productEndTimes[productNumber];
                var remainingTime = endTime - DateTime.Now;
                return remainingTime.TotalSeconds > 0 ? remainingTime : TimeSpan.Zero;
            }
            return TimeSpan.Zero;
        }
        // Hämta totalusers för en specifik produkt från Firebase
        public async Task UpdateTotalUsers(int productNumber, string userEmail)
        {
            if (string.IsNullOrEmpty(userEmail))
            {
                Console.WriteLine("User email is null or empty, cannot update total users.");
                return;
            }

            // Hämta det låsta beloppet från användarens individuella fil
            var individualUserData = await GetUserFromFirebase(productNumber, userEmail);

            if (individualUserData == null)
            {
                Console.WriteLine($"No individual lock-in data found for {userEmail}, skipping update.");
                return;
            }

            double lockInAmount = individualUserData.LockInAmount;

            // Hämta nuvarande totalusers.json data för produkten
            var totalUsers = await GetTotalUsersFromFirebase(productNumber);

            // Uppdatera eller sätt användarens belopp till exakt belopp från den individuella filen
            totalUsers[userEmail] = lockInAmount;

            // Serialisera och spara uppdaterad total users data tillbaka till totalusers.json
            var usersJson = JsonSerializer.Serialize(totalUsers, new JsonSerializerOptions { WriteIndented = false });
            var usersContent = new StringContent(usersJson, Encoding.UTF8, "application/json");
            var usersPath = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2Ftotalusers.json";

            var response = await _httpClient.PostAsync(usersPath, usersContent);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to update total users for product {productNumber}. Error: {response.StatusCode}");
            }
            else
            {
                Console.WriteLine($"Total users for product {productNumber} updated successfully.");
            }
        }


        private string GetFirebasePath(int productNumber, string endpoint)
        {
            return $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2F{endpoint}";
        }

        private async Task<ProductData> GetUserFromUserStats(string userEmail)
        {
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FUserStats%2F{userEmail}.json?alt=media";
            var response = await _httpClient.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                try
                {
                    // Deserialize JSON to a ProductData object
                    var user = JsonSerializer.Deserialize<ProductData>(jsonResponse);
                    return user;
                }
                catch (JsonException)
                {
                    Console.WriteLine("Failed to deserialize user data.");
                    return null;
                }
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Return null if the user doesn't exist
                return null;
            }
            else
            {
                Console.WriteLine($"Failed to fetch user data for {userEmail}. Status code: {response.StatusCode}");
                return null;
            }
        }

        // Method to fetch totalusers.json from Firebase
        public async Task<Dictionary<string, double>> GetTotalUsersFromFirebase(int productNumber)
        {
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2Ftotalusers.json?alt=media";
            var response = await _httpClient.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Dictionary<string, double>>(jsonResponse);
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new Dictionary<string, double>();  // Return an empty dictionary if file doesn't exist
            }
            else
            {
                Console.WriteLine($"Error fetching total users for product {productNumber}: {response.StatusCode}");
                return new Dictionary<string, double>();  // Return an empty dictionary on error
            }
        }


        // Helper method to check if totalusers.json exists
        private async Task<bool> CheckIfTotalUsersFileExists(int productNumber)
        {
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2Ftotalusers.json?alt=media";
            var response = await _httpClient.GetAsync(path);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;  // File does not exist
            }
            else if (response.IsSuccessStatusCode)
            {
                return true;  // File exists
            }
            else
            {
                Console.WriteLine($"Error checking if totalusers file exists: {response.StatusCode}");
                return false;
            }
        }

        // Nollställ totalusers för en produkt (t.ex. efter att en produkt har löpt ut eller avslutats)
        public async Task ResetTotalUsers(int productNumber)
        {
            var totalUsers = new TotalUsers(); // Skapa en tom totalusers-instans

            var usersJson = JsonSerializer.Serialize(totalUsers);
            var content = new StringContent(usersJson, Encoding.UTF8, "application/json");

            // Skicka PUT-förfrågan för att nollställa totalusers i Firebase
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2Ftotalusers.json";

            try
            {
                var response = await _httpClient.PostAsync(path, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Total users for product {productNumber} reset successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to reset total users for product {productNumber}. Error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting total users for product {productNumber}: {ex.Message}");
            }
        }

        public class ProductData
        {
            public int ProductNumber { get; set; }
            public string UserEmail { get; set; }
            public double Price { get; set; }
            public string Winner { get; set; }
            public double LockInAmount { get; set; }
            public int LockInCount { get; set; }
            public double TotalLockInAmount { get; set; }
            public DateTime Timestamp { get; set; }
            public string ProductName { get; set; }
            public string ImageUrl { get; set; }
            public bool IsJackpot { get; set; } // Nytt fält för att markera om produkten är en jackpot
            public double PrizePool { get; set; } = 0; // För att spåra jackpot-belopp
            public List<string> Participants { get; set; } = new List<string>();
        }
       

        public class UserProfile
        {
            public string UserKey { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
        }
        public class TotalUsers
        {
            public Dictionary<string, List<double>> Users { get; set; } = new Dictionary<string, List<double>>();
        }

    }
}
