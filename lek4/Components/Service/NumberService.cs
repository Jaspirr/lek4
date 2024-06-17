using System.Collections.Generic;

namespace lek4.Components.Service
{
    public class NumberService
    {
        public double CurrentNumber { get; set; }
        public List<int> UnlockedProducts { get; private set; } = new List<int>();
        public List<int> LockedInProducts { get; private set; } = new List<int>();

        // A dictionary to keep track of how many people have locked in each product and the amounts
        public Dictionary<int, List<double>> ProductLockAmounts { get; private set; } = new Dictionary<int, List<double>>();

        public void UnlockProduct(int productNumber)
        {
            if (!UnlockedProducts.Contains(productNumber))
            {
                UnlockedProducts.Add(productNumber);
            }
        }

        public void LockInProduct(int productNumber, double amount)
        {
            if (!LockedInProducts.Contains(productNumber))
            {
                LockedInProducts.Add(productNumber);
            }

            if (ProductLockAmounts.ContainsKey(productNumber))
            {
                ProductLockAmounts[productNumber].Add(amount);
            }
            else
            {
                ProductLockAmounts[productNumber] = new List<double> { amount };
            }

            // Deduct the locked in amount from the current number
            CurrentNumber -= amount;
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
    }


}
