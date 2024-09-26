using System.Collections.Generic;

namespace lek4.Components.Service
{
    public class ProductDayCounterService
    {
        // Dictionary to store day counters for products
        private Dictionary<int, int> productDayCounters = new Dictionary<int, int>();

        // Method to get the day counter for a product
        public int GetDayCounter(int productNumber)
        {
            if (productDayCounters.ContainsKey(productNumber))
            {
                return productDayCounters[productNumber];
            }
            return 1; // Default value if no day counter exists for the product
        }

        // Method to set the day counter for a product
        public void SetDayCounter(int productNumber, int day)
        {
            if (productDayCounters.ContainsKey(productNumber))
            {
                productDayCounters[productNumber] = day;
            }
            else
            {
                productDayCounters.Add(productNumber, day);
            }
        }
    }
}
