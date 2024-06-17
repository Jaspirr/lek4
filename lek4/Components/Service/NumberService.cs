using System.Collections.Generic;

namespace lek4.Components.Service
{
    public class NumberService
    {
        public double CurrentNumber { get; set; }

        public List<int> UnlockedProducts { get; private set; } = new List<int>();
        public List<int> LockedInProducts { get; private set; } = new List<int>();

        public void UnlockProduct(int productNumber)
        {
            if (!UnlockedProducts.Contains(productNumber))
            {
                UnlockedProducts.Add(productNumber);
            }
        }

        public void LockInProduct(int productNumber)
        {
            if (!LockedInProducts.Contains(productNumber))
            {
                LockedInProducts.Add(productNumber);
            }
        }

        public bool IsProductUnlocked(int productNumber)
        {
            return UnlockedProducts.Contains(productNumber);
        }

        public bool IsProductLockedIn(int productNumber)
        {
            return LockedInProducts.Contains(productNumber);
        }
    }
}
