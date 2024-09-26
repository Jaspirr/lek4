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
        // Placeholder för logik som hämtar vinnaren för en specifik produkt
        return "Winner:" + productNumber;
    }

    public TimeSpan GetRemainingTime(int productNumber)
    {
        // Använder den nya metoden i ProductService för att hämta återstående tid
        return _productService.GetTimeRemaining(productNumber);
    }

    public DayOfWeek GetDrawDay(int productNumber)
    {
        // Använder ProductService för att hämta dagen för dragningen
        return _productService.GetDay(productNumber);
    }
}
