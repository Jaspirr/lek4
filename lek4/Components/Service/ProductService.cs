using System;
using System.Collections.Generic;

namespace lek4.Components.Service
{
    public class ProductService
    {
        private Dictionary<int, double> productPrices = new Dictionary<int, double>();
        private Dictionary<int, DayOfWeek> productDays = new Dictionary<int, DayOfWeek>();
        private Dictionary<int, DateTime> productEndTimes = new Dictionary<int, DateTime>();
        private Dictionary<int, List<string>> lockedInUsers = new Dictionary<int, List<string>>(); // Håller reda på användare som har låst in sig
        private Dictionary<int, string> productWinners = new Dictionary<int, string>(); // Håller reda på vinnaren för en produkt
        private List<int> productNumbers = new List<int>();

        public ProductService()
        {
            // Exempel: Lägg till standardprodukter vid initialisering
            productNumbers = new List<int> { 1, 2, 3 }; // Du kan anpassa detta

            // Exempel på att sätta sluttider för produkter
            productEndTimes[1] = DateTime.Now.AddDays(2); // Produkt 1 slutar om 2 dagar
            productEndTimes[2] = DateTime.Now.AddHours(10); // Produkt 2 slutar om 10 timmar
            productEndTimes[3] = DateTime.Now.AddMinutes(30); // Produkt 3 slutar om 30 minuter
        }

        // Hämta lista över alla produktnummer
        public List<int> GetProductNumbers()
        {
            return productNumbers;
        }

        public double GetPrice(int productNumber)
        {
            if (productPrices.ContainsKey(productNumber))
            {
                return productPrices[productNumber];
            }
            return productNumber * 10;
        }

        public void SetPrice(int productNumber, double price)
        {
            if (productPrices.ContainsKey(productNumber))
            {
                productPrices[productNumber] = price;
            }
            else
            {
                productPrices.Add(productNumber, price);
            }
        }

        public DayOfWeek GetDay(int productNumber)
        {
            if (productDays.ContainsKey(productNumber))
            {
                return productDays[productNumber];
            }
            return DayOfWeek.Friday;
        }

        public void SetDay(int productNumber, DayOfWeek day)
        {
            if (productDays.ContainsKey(productNumber))
            {
                productDays[productNumber] = day;
            }
            else
            {
                productDays.Add(productNumber, day);
            }
        }

        // Ny metod för att hämta återstående tid för en produkt
        public TimeSpan GetTimeRemaining(int productNumber)
        {
            if (productEndTimes.ContainsKey(productNumber))
            {
                var endTime = productEndTimes[productNumber];
                var remainingTime = endTime - DateTime.Now;

                // Returnera återstående tid om produkten inte har gått ut
                if (remainingTime.TotalSeconds > 0)
                {
                    return remainingTime;
                }
                else
                {
                    // Om tiden har gått ut, returnera 0
                    return TimeSpan.Zero;
                }
            }

            // Om produkten inte hittas, returnera 0
            return TimeSpan.Zero;
        }

        // Lägg till en användare som har låst in på en produkt
        public void LockInUser(int productNumber, string userId)
        {
            if (!lockedInUsers.ContainsKey(productNumber))
            {
                lockedInUsers[productNumber] = new List<string>();
            }

            // Ensure that only unique users are added to the list for a product
            if (!lockedInUsers[productNumber].Contains(userId))
            {
                lockedInUsers[productNumber].Add(userId);
            }
        }

        // Hämta användare som har låst in på en produkt
        public List<string> GetLockedInUsers(int productNumber)
        {
            if (lockedInUsers.ContainsKey(productNumber))
            {
                return lockedInUsers[productNumber];
            }
            return new List<string>();
        }

        // Hämta vinnaren för en produkt
        public string GetWinner(int productNumber)
        {
            if (productWinners.ContainsKey(productNumber))
            {
                return productWinners[productNumber];
            }
            return null;
        }

        // Slumpa vinnare för en produkt från användare som har låst in
        public void DrawWinner(int productNumber)
        {
            var users = GetLockedInUsers(productNumber);
            if (users.Count > 0)
            {
                var random = new Random();
                var winner = users[random.Next(users.Count)];
                productWinners[productNumber] = winner;

                // Log or display a message to confirm draw is working
                Console.WriteLine($"Winner for product {productNumber} is: {winner}");
            }
            else
            {
                Console.WriteLine($"No users locked in for product {productNumber}");
            }
        }

        // Lägg till en ny produkt
        public void AddProduct(int productNumber)
        {
            if (!productNumbers.Contains(productNumber))
            {
                productNumbers.Add(productNumber);
            }
        }

        // Ta bort en produkt
        public void RemoveProduct(int productNumber)
        {
            if (productNumbers.Contains(productNumber))
            {
                productNumbers.Remove(productNumber);
                // Ta också bort relaterad data om den finns
                productPrices.Remove(productNumber);
                productDays.Remove(productNumber);
                productEndTimes.Remove(productNumber); // Ta bort sluttiden
                lockedInUsers.Remove(productNumber); // Ta bort låsta användare
                productWinners.Remove(productNumber); // Ta bort eventuella vinnare
            }
        }
    }
}
