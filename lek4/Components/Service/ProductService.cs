using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System;

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

        private readonly HttpClient _httpClient;

        public ProductService(HttpClient httpClient)
        {
            _httpClient = httpClient;
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
        public void DrawWinner(int productNumber)
        {
            if (lockedInUsers.ContainsKey(productNumber) && lockedInUsers[productNumber].Count > 0)
            {
                var users = lockedInUsers[productNumber];
                var random = new Random();
                var winner = users[random.Next(users.Count)];
                productWinners[productNumber] = winner; // Store the winner in the productWinners dictionary
                Console.WriteLine($"Winner for product {productNumber} is {winner}");
            }
            else
            {
                Console.WriteLine($"No users locked in for product {productNumber}. No winner drawn.");
            }
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

        public void LockInUser(int productNumber, string userEmail)
        {
            if (!lockedInUsers.ContainsKey(productNumber))
            {
                lockedInUsers[productNumber] = new List<string>();
            }

            if (!lockedInUsers[productNumber].Contains(userEmail))
            {
                lockedInUsers[productNumber].Add(userEmail);
                Console.WriteLine($"User {userEmail} locked in for product {productNumber}.");
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
    }
}
