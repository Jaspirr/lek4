using System;
using System.Collections.Generic;

namespace lek4.Components.Service
{
    public class ProductDrawDateService
    {
        // Dictionary to store draw dates for products
        private Dictionary<int, DateTime> productDrawDates = new Dictionary<int, DateTime>();

        // Method to get the draw date for a product
        public DateTime? GetDrawDate(int productNumber)
        {
            if (productDrawDates.TryGetValue(productNumber, out DateTime drawDate))
            {
                return drawDate;
            }
            return null; // Return null if no draw date exists for the product
        }

        // Method to set the draw date for a product
        public void SetDrawDate(int productNumber, DateTime drawDate)
        {
            productDrawDates[productNumber] = drawDate;
        }

        // Method to check if a product has a draw date set
        public bool HasDrawDate(int productNumber)
        {
            return productDrawDates.ContainsKey(productNumber);
        }

        // Method to remove the draw date for a product
        public void RemoveDrawDate(int productNumber)
        {
            productDrawDates.Remove(productNumber);
        }
    }
}
