using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System;
using Blazored.LocalStorage;

namespace lek4.Components.Service
{
    public class ProductService
    {

        private Dictionary<int, double> productPrices = new Dictionary<int, double>();
        private Dictionary<int, DayOfWeek> productDays = new Dictionary<int, DayOfWeek>();
        private Dictionary<int, DateTime> productEndTimes = new Dictionary<int, DateTime>();
        private Dictionary<int, List<string>> lockedInUsers = new Dictionary<int, List<string>>(); // Keep track of users locked into products
        private Dictionary<int, string> productWinners = new Dictionary<int, string>(); // Keep track of product winners
        private List<ProductData> products = new List<ProductData>();
        private readonly UserService _userService;
        private readonly ILocalStorageService _localStorage;

        private readonly HttpClient _httpClient;


        public ProductService(HttpClient httpClient,UserService userService, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _userService = userService;
            _localStorage = localStorage;
            // Example: Pre-define some product numbers
            productEndTimes[1] = DateTime.Now.AddDays(2); // Product 1 ends in 2 days
            productEndTimes[2] = DateTime.Now.AddHours(10); // Product 2 ends in 10 hours
        }

        public List<int> GetProductNumbers()
        {
            return products.Select(p => p.ProductNumber).ToList(); // Return product numbers
        }

        public async Task AddProductToFirebase(int productNumber, double price, string userEmail)
        {
            var lockInAmount = 0.0; // Set to 0 or any desired initial value

            var productData = new ProductData
            {
                ProductNumber = productNumber,
                Price = price,
                UserEmail = userEmail,
                LockInAmount = lockInAmount
            };

            // Call SaveProductData on the current instance
            await SaveProductData(productNumber, userEmail, lockInAmount, price);
        }

       
        public void RemoveProductLocally(int productNumber)
        {
            var product = products.FirstOrDefault(p => p.ProductNumber == productNumber);
            if (product != null)
            {
                products.Remove(product);
                Console.WriteLine($"Product {productNumber} removed locally.");
            }
        }
        public List<ProductData> GetProducts()
        {
            return products;
        }

        public async Task RemoveProductFromFirebase(int productNumber)
        {
            // Define the Firebase Storage path for the product
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2F{productNumber}.json";


            // Make a DELETE request to remove the product data from Firebase
            var response = await _httpClient.DeleteAsync(path);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Product {productNumber} removed from Firebase.");
            }
            else
            {
                Console.WriteLine($"Failed to remove product {productNumber} from Firebase.");
            }
        }
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

            // Firebase path (without ?alt=media for uploading)
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2F{productNumber}.json";

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



        public async Task<List<ProductData>> FetchAllProductsFromFirebaseAsync(int maxProducts = 100)
        {
            var allProducts = new List<ProductData>();
            int productNumber = 1; // Start with the first product

            while (productNumber <= maxProducts)
            {
                var product = await GetProductFromFirebaseAsync(productNumber);

                if (product != null)
                {
                    allProducts.Add(product);
                    productNumber++; // Move to the next product number
                }
                else
                {
                    // Stop fetching when the product is not found (HTTP 404)
                    break;
                }
            }

            return allProducts;
        }

        public async Task<ProductData> GetProductFromFirebaseAsync(int productNumber)
        {
            // Construct the Firebase URL for the specific product
            var url = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2F{productNumber}.json?alt=media";

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
                // Log if the product is not found
                Console.WriteLine($"Failed to fetch product {productNumber}. Status code: {response.StatusCode}");
                return null;
            }
        }

        public async Task<List<int>> GetProductNumbersFromFirebaseAsync(int maxProducts = 100)
        {
            var productNumbers = new List<int>();

            // Iterate over a range of possible product numbers and fetch each product
            for (int productNumber = 1; productNumber <= maxProducts; productNumber++)
            {
                var product = await GetProductFromFirebaseAsync(productNumber);

                if (product != null)
                {
                    productNumbers.Add(product.ProductNumber); // Add the product number to the list
                }
                else
                {
                    // If a product is not found, we stop fetching further products.
                    // You can adjust this logic if needed.
                    break;
                }
            }

            return productNumbers;
        }


        public double GetPrice(int productNumber)
        {
            return productPrices.ContainsKey(productNumber) ? productPrices[productNumber] : productNumber * 10;
        }

        public void SetPrice(int productNumber, double price)
        {
            productPrices[productNumber] = price;
        }

        public TimeSpan GetTimeRemaining(int productNumber)
        {
            if (productEndTimes.ContainsKey(productNumber))
            {
                var remainingTime = productEndTimes[productNumber] - DateTime.Now;
                return remainingTime.TotalSeconds > 0 ? remainingTime : TimeSpan.Zero;
            }
            return TimeSpan.Zero;
        }


        public async Task LockInUser(int productNumber, string userEmail, double lockInAmount)
        {
            if (string.IsNullOrEmpty(userEmail) || userEmail == "Anonymous")
            {
                Console.WriteLine("Invalid email. Cannot lock in as Anonymous.");
                return;
            }

            // Fetch the existing list of users from Firebase
            var existingUsers = await GetLockedInUsersFromFirebase(productNumber);

            // Check if the user is already locked in
            var existingUser = existingUsers.FirstOrDefault(u => u.UserEmail == userEmail);

            if (existingUser != null)
            {
                // Update the lock-in amount for the existing user
                existingUser.LockInAmount += lockInAmount;
                Console.WriteLine($"Updated lock-in amount for {userEmail} on product {productNumber}");
            }
            else
            {
                // Create a new user entry
                var newUser = new ProductData
                {
                    ProductNumber = productNumber,
                    UserEmail = userEmail,
                    LockInAmount = lockInAmount
                };
                existingUsers.Add(newUser);
            }

            // Save the updated list of users back to Firebase
            await SaveLockedInUsersToFirebase(productNumber, existingUsers);
        }


        private async Task<List<ProductData>> GetLockedInUsersFromFirebase(int productNumber)
        {
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2F{productNumber}.json?alt=media";
            var response = await _httpClient.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();

                try
                {
                    // Try to deserialize as a list of ProductData
                    var lockedInUsers = JsonSerializer.Deserialize<List<ProductData>>(jsonResponse);
                    return lockedInUsers ?? new List<ProductData>(); // Return empty list if null
                }
                catch (JsonException)
                {
                    Console.WriteLine("Failed to deserialize JSON into List<ProductData>. Make sure the JSON is an array.");
                    return new List<ProductData>(); // Return an empty list on failure
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


        public async Task<UserProfile> GetUserProfileFromFirebase(string userEmail)
        {
            // Firebase URL baserad på e-postadressen
            var url = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2F{userEmail}.json?alt=media";

            // Skicka en HTTP GET-förfrågan till Firebase
            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                // Läs och deserialisera svaret till ett UserProfile-objekt
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var userProfile = JsonSerializer.Deserialize<UserProfile>(jsonResponse);
                return userProfile;
            }
            else
            {
                Console.WriteLine($"Failed to fetch user profile for {userEmail}. Status code: {response.StatusCode}");
                return null;
            }
        }
        private async Task SaveLockedInUsersToFirebase(int productNumber, List<ProductData> users)
        {
            // Serialize the list of users
            var lockedInUsersData = JsonSerializer.Serialize(users);
            var content = new StringContent(lockedInUsersData, Encoding.UTF8, "application/json");

            // Save the updated list of locked-in users to Firebase
            var response = await _httpClient.PutAsync($"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2F{productNumber}.json", content);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to save locked-in users for product {productNumber}. Error: {response.StatusCode}");
            }
        }



        public async Task<List<(string UserEmail, double LockInAmount)>> GetLockedInUsersWithLockInAmountAsync(int productNumber)
        {
            // Fetch the existing locked-in users from Firebase
            var lockedInUsers = await GetLockedInUsersFromFirebase(productNumber);  // Must be fetched from Firebase

            // Create and return a list of (UserEmail, LockInAmount)
            return lockedInUsers.Select(user => (user.UserEmail, user.LockInAmount)).ToList();
        }



        public async Task<string> GetWinnerFromFirebase(int productNumber)
        {
            // Firebase path for fetching winner data
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users/winner/{productNumber}/winner.json?alt=media";

            // Send GET request to Firebase
            var response = await _httpClient.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var winnerData = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonResponse);
                return winnerData.ContainsKey("Winner") ? winnerData["Winner"] : null;
            }
            else
            {
                Console.WriteLine($"Failed to fetch winner for product {productNumber}. Status code: {response.StatusCode}");
                return null;
            }
        }



        // Step 3: Method to fetch detailed winner profile from Firebase using email
        public async Task<string> GetWinnerProfileFromFirebase(string winnerEmail)
        {
            // Firebase path for fetching the winner's profile
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2F{winnerEmail}%2Fwinner.json?alt=media";

            // Send GET request to Firebase
            var response = await _httpClient.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var winnerProfile = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonResponse);

                return winnerProfile.ContainsKey("Winner") ? winnerProfile["Winner"] : null;
            }
            else
            {
                Console.WriteLine($"Failed to fetch winner profile for {winnerEmail}. Status code: {response.StatusCode}");
                return null;
            }
        }


        public List<string> GetLockedInUsers(int productNumber)
        {
            return lockedInUsers.ContainsKey(productNumber) ? lockedInUsers[productNumber] : new List<string>();
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
            // Add other user properties as needed
        }
    }
}
