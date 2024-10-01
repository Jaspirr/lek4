using System.Collections.Generic;

namespace lek4.Components.Service
{
    public class NumberService
    {
        public double CurrentNumber { get; set; }
        public int MaxProductNumber { get; set; } = 9; // Set a default max product number
        public List<int> UnlockedProducts { get; private set; } = new List<int>();
        public List<int> LockedInProducts { get; private set; } = new List<int>();

        // A dictionary to keep track of how many people have locked in each product and the amounts
        public Dictionary<int, List<double>> ProductLockAmounts { get; private set; } = new Dictionary<int, List<double>>();
        public Dictionary<int, Dictionary<string, double>> ProductUserLocks { get; private set; } = new Dictionary<int, Dictionary<string, double>>();

        public void UnlockProduct(int productNumber)
        {
            if (!UnlockedProducts.Contains(productNumber))
            {
                UnlockedProducts.Add(productNumber);
            }
        }

        public void LockInProduct(int productNumber, string userEmail, double amount)
        {
            if (!LockedInProducts.Contains(productNumber))
            {
                LockedInProducts.Add(productNumber);
            }

            if (!ProductLockAmounts.ContainsKey(productNumber))
            {
                ProductLockAmounts[productNumber] = new List<double>();
            }
            ProductLockAmounts[productNumber].Add(amount);

            // Deduct the locked-in amount from the current number
            CurrentNumber -= amount;

            // Track user lock-in by userEmail
            if (!ProductUserLocks.ContainsKey(productNumber))
            {
                ProductUserLocks[productNumber] = new Dictionary<string, double>();
            }

            // Add or update the user's locked-in amount
            ProductUserLocks[productNumber][userEmail] = amount;

            Console.WriteLine($"{userEmail} locked in {amount} on product {productNumber}");
        }
        public bool IsProductUnlocked(int productNumber)
        {
            return UnlockedProducts.Contains(productNumber);
        }

        public bool IsProductLockedIn(int productNumber)
        {
            return LockedInProducts.Contains(productNumber);
        }

        // Calculate the percentage chance of winning for a product based on lock-in amounts
        public double GetWinningChance(int productNumber)
        {
            if (ProductLockAmounts.TryGetValue(productNumber, out List<double> lockAmounts))
            {
                double totalLockAmount = lockAmounts.Sum();
                // For simplicity, let's assume the chance is calculated as (1 / totalLockAmount) * 100
                return 1.0 / totalLockAmount * 100;
            }
            return 0;
        }

        // Check if the user has enough odds left
        public bool HasEnoughOdds(double amount)
        {
            return CurrentNumber >= amount;
        }

        // Get the remaining odds
        public double GetRemainingOdds()
        {
            return CurrentNumber;
        }

    }

}
