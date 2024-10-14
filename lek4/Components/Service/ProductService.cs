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
        private readonly UserService _userService;
        private readonly ILocalStorageService _localStorage;
        private Dictionary<int, DateTime> productEndTimes = new Dictionary<int, DateTime>();


        public ProductService(HttpClient httpClient, UserService userService, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _userService = userService;
            _localStorage = localStorage;
        }

        // Add a new product to Firebase with productInfo.json
        public async Task AddProductToFirebase(int productNumber, double price, string userEmail)
        {
            // Define the lock-in amount
            double lockInAmount = 0.0; // Initial lock-in amount

            // Call SaveProductData with all the required arguments
            await SaveProductData(productNumber, userEmail, lockInAmount, price);
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
        public async Task SaveProductData(int productNumber, string userEmail, double lockInAmount, double price)
        {
            var productData = new ProductData
            {
                ProductNumber = productNumber,
                UserEmail = userEmail,
                LockInAmount = lockInAmount,
                Price = price
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
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/products/{productNumber}/winner.json?alt=media";

            // Send a GET request to Firebase
            var response = await _httpClient.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                // Deserialize the response content to a dictionary
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var winnerData = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonResponse);

                // Return the winner's email if it exists in the response
                return winnerData.ContainsKey("Winner") ? winnerData["Winner"] : null;
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
                // Construct the Firebase URL for the specific product (simplified and without the extra encoded slashes)
                var url = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2FproductInfo.json?alt=media";

                // Send an HTTP GET request to Firebase
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    // Read and deserialize the response into a ProductData object
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var product = JsonSerializer.Deserialize<ProductData>(jsonResponse);

                    return product;
                }
                else
                {
                    // Log if the product is not found or any other error
                    Console.WriteLine($"Failed to fetch product {productNumber}. Status code: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                // Log if there’s any other error
                Console.WriteLine($"An error occurred while fetching product {productNumber}: {ex.Message}");
                return null;
            }
        }



        // Remove a product from Firebase
        public async Task RemoveProductFromFirebase(int productNumber)
        {
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users/products/product{productNumber}/productInfo.json";

            var response = await _httpClient.DeleteAsync(path);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Product {productNumber} removed from Firebase.");
            }
            else
            {
                Console.WriteLine($"Failed to remove product {productNumber} from Firebase. Status code: {response.StatusCode}");
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

            // Hämta nuvarande lock-in count (användarantal) för den specifika produkten
            int userNumber = await GetLockInCount(productNumber) + 1; // Ökar med 1 för nästa användare

            // Skapa användardata
            var userLockInData = new ProductData
            {
                ProductNumber = productNumber,
                UserEmail = userEmail,
                LockInAmount = lockInAmount
            };

            // Serialisera användardatan till JSON
            var userJson = JsonSerializer.Serialize(userLockInData);
            var content = new StringContent(userJson, Encoding.UTF8, "application/json");

            // Ange Firebase-sökvägen för att spara user{userNumber}.json
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users/products/product{productNumber}/user{userNumber}.json";

            var response = await _httpClient.PutAsync(path, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Lock-in for {userEmail} saved successfully for product {productNumber} as user{userNumber}.json.");

                // Uppdatera antalet användare som har lockat in
                await UpdateLockInCount(productNumber, userNumber);
            }
            else
            {
                Console.WriteLine($"Failed to save lock-in for {userEmail} on product {productNumber}. Error: {response.StatusCode}");
            }
        }

        public async Task<int> GetLockInCount(int productNumber)
        {
            try
            {
                // Firebase path för att hämta lock-in count
                var url = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users/products/product{productNumber}/lockInCount.json?alt=media";

                // Skicka en GET-förfrågan till Firebase
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

        public async Task UpdateLockInCount(int productNumber, int newLockInCount)
        {
            try
            {
                // Firebase path för att uppdatera lock-in count
                var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users/products/product{productNumber}/lockInCount.json";

                // Serialisera det nya lock-in count till JSON
                var countJson = JsonSerializer.Serialize(newLockInCount);
                var content = new StringContent(countJson, Encoding.UTF8, "application/json");

                // Skicka en PUT-förfrågan för att uppdatera lock-in count
                var response = await _httpClient.PutAsync(path, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Lock-in count for product {productNumber} updated to {newLockInCount}.");
                }
                else
                {
                    Console.WriteLine($"Failed to update lock-in count for product {productNumber}. Error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating lock-in count: {ex.Message}");
            }
        }




        // Fetch locked-in users from Firebase
        private async Task<List<ProductData>> GetLockedInUsersFromFirebase(int productNumber)
        {
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users/products/product{productNumber}/lockedInUsers.json?alt=media";
            var response = await _httpClient.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ProductData>>(jsonResponse) ?? new List<ProductData>();
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Return an empty list if no locked-in users are found
                return new List<ProductData>();
            }
            else
            {
                Console.WriteLine($"Failed to fetch locked-in users for product {productNumber}. Status code: {response.StatusCode}");
                return new List<ProductData>();
            }
        }


        // Save the list of locked-in users to Firebase
       private async Task SaveLockedInUsersToFirebase(int productNumber, List<ProductData> users)
        {
            var usersJson = JsonSerializer.Serialize(users);
            var content = new StringContent(usersJson, Encoding.UTF8, "application/json");

            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users/products/product{productNumber}/lockedInUsers.json";
            var response = await _httpClient.PutAsync(path, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Locked-in users for product {productNumber} saved successfully.");
            }
            else
            {
                Console.WriteLine($"Failed to save locked-in users for product {productNumber}. Error: {response.StatusCode}");
            }
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




        public class ProductData
        {
            public int ProductNumber { get; set; }
            public string UserEmail { get; set; }
            public double Price { get; set; }
            public double LockInAmount { get; set; }
        }

        public class UserProfile
        {
            public string UserKey { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
        }
    }
}
