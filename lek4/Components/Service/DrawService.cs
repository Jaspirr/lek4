using lek4.Components.Service;

public class DrawService
{
    private readonly NumberService _numberService;
    private readonly ProductService _productService;

    public DrawService(NumberService numberService, ProductService productService)
    {
        _numberService = numberService;
        _productService = productService;
    }

    public string GetWinner(int productNumber)
    {
        // Placeholder logic to fetch or select a winner
        var users = _productService.GetLockedInUsers(productNumber);
        if (users.Count > 0)
        {
            var random = new Random();
            var winner = users[random.Next(users.Count)];
            return $"Winner: {winner}";
        }
        return "No winner selected.";
    }
    public void DrawWinner(int productNumber)
    {
        // Call ProductService's DrawWinner method
        _productService.DrawWinner(productNumber);
    }
    public TimeSpan GetRemainingTime(int productNumber)
    {
        // Use ProductService to get the remaining time
        return _productService.GetTimeRemaining(productNumber);
    }
}