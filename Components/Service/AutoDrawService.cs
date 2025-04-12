using lek4.Components.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

public class AutoDrawService
{
    private readonly ProductService _productService;
    private readonly DrawService _drawService;
    private readonly Dictionary<int, System.Timers.Timer> _timers = new Dictionary<int, System.Timers.Timer>();
    private readonly HttpClient _httpClient;
    private readonly DrawJackpotService _drawJackpotService;

    public AutoDrawService(ProductService productService, DrawService drawService, HttpClient httpClient, DrawJackpotService drawJackpotService)
    {
        _productService = productService;
        _drawService = drawService;
        _httpClient = httpClient;
        _drawJackpotService = drawJackpotService; // Lägg till detta
    }

    public async Task InitializeDrawTimersAsync()
    {
        Console.WriteLine("Initializing draw timers...");

        // Hämta alla produkter
        var products = await _productService.FetchAllProductsFromFirebaseAsync();

        foreach (var product in products)
        {
            try
            {
                DateTime currentUtcDate = DateTime.UtcNow.Date;
                DateTime productDrawUtcDate = product.DrawDate.ToUniversalTime().Date;

                // Kontrollera om produkten är en jackpot
                if (product.IsJackpot)
                {
                    Console.WriteLine($"Product {product.ProductNumber} is a jackpot. Processing as jackpot.");
                    await ProcessJackpotProductAsync(product);
                    continue;
                }

                // Kontrollera om dragningen redan är utförd idag
                if (await HasDrawBeenPerformedTodayAsync(product.ProductNumber))
                {
                    Console.WriteLine($"Skipping product {product.ProductNumber}, already drawn today.");
                    continue;
                }

                // Kontrollera om dragningens datum matchar dagens datum
                if (productDrawUtcDate == currentUtcDate)
                {
                    Console.WriteLine($"[INFO] Product {product.ProductNumber}: Draw is scheduled for today at {product.DrawDate.ToUniversalTime().TimeOfDay} UTC.");
                    ScheduleDrawTimerForToday(product);
                }
                else if (productDrawUtcDate < currentUtcDate)
                {
                    Console.WriteLine($"[WARNING] Product {product.ProductNumber}: Draw date {productDrawUtcDate} has already passed.");

                    if (!await HasDrawBeenPerformedTodayAsync(product.ProductNumber))
                    {
                        Console.WriteLine($"No winner has been drawn for product {product.ProductNumber}. Performing draw now.");
                        await PerformDrawAsync(product);
                    }
                    else
                    {
                        Console.WriteLine($"Winner already drawn for product {product.ProductNumber}. Skipping.");
                    }
                }
                else
                {
                    Console.WriteLine($"[INFO] Product {product.ProductNumber}: Draw date {productDrawUtcDate} is in the future. Skipping.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error handling product {product.ProductNumber}: {ex.Message}");
            }
        }
    }

    private async Task ProcessJackpotProductAsync(ProductService.ProductData product)
    {
        Console.WriteLine($"Processing jackpot for product {product.ProductNumber}...");

        try
        {
            // ✅ Kontrollera om dragningen skulle ske idag ELLER om den har missats
            bool shouldPerformDraw = await IsJackpotDrawDayOrMissedAsync(product);

            if (!shouldPerformDraw)
            {
                Console.WriteLine($"[INFO] Product {product.ProductNumber}: Draw is NOT scheduled for today and has not been missed. Skipping jackpot draw.");
                return;
            }

            // ✅ Kontrollera om winner.json redan finns
            if (await HasWinnerBeenDeclaredAsync(product.ProductNumber))
            {
                Console.WriteLine($"Winner already declared for jackpot product {product.ProductNumber}. Skipping draw.");
                return;
            }

            // ✅ Hämta jackpotbiljetter
            var tickets = await _drawJackpotService.GetJackpotTickets();
            if (!tickets.Any())
            {
                Console.WriteLine($"No tickets found for jackpot product {product.ProductNumber}. Skipping.");
                return;
            }

            // ✅ Generera en slumpmässig vinnande biljett
            var winningTicket = _drawJackpotService.GenerateRandomTicket();

            // ✅ Hitta vinnaren
            string winnerEmail = tickets
                .Where(ticket => _drawJackpotService.IsMatchingTicket(ticket, winningTicket))
                .Select(ticket => ticket.UserEmail)
                .FirstOrDefault() ?? "No winner";

            // ✅ Spara resultatet i Firebase
            await _drawJackpotService.SaveWinnerToFirebase(product.ProductNumber, winnerEmail, DateTime.UtcNow, winningTicket);

            Console.WriteLine($"Jackpot draw completed for product {product.ProductNumber}. Winner: {winnerEmail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing jackpot for product {product.ProductNumber}: {ex.Message}");
        }
    }

    private async Task<bool> IsJackpotDrawDayAsync(ProductService.ProductData product)
    {
        DateTime currentUtcDate = DateTime.UtcNow.Date;
        DateTime productDrawUtcDate = product.DrawDate.ToUniversalTime().Date;

        Console.WriteLine($"[DEBUG] Checking jackpot draw date for product {product.ProductNumber}");
        Console.WriteLine($"[DEBUG] Current UTC Date: {currentUtcDate}");
        Console.WriteLine($"[DEBUG] Product Draw UTC Date: {productDrawUtcDate}");

        return productDrawUtcDate == currentUtcDate;
    }
    private async Task<bool> IsJackpotDrawDayOrMissedAsync(ProductService.ProductData product)
    {
        DateTime currentUtcDate = DateTime.UtcNow.Date;
        DateTime productDrawUtcDate = product.DrawDate.ToUniversalTime().Date;

        Console.WriteLine($"[DEBUG] Checking jackpot draw date for product {product.ProductNumber}");
        Console.WriteLine($"[DEBUG] Current UTC Date: {currentUtcDate}");
        Console.WriteLine($"[DEBUG] Product Draw UTC Date: {productDrawUtcDate}");

        if (productDrawUtcDate == currentUtcDate)
        {
            Console.WriteLine($"[INFO] Today is the draw date for product {product.ProductNumber}.");
            return true;
        }

        // ✅ Om DrawDate har passerat, kolla om vi har missat dragningen
        if (productDrawUtcDate < currentUtcDate)
        {
            bool hasDrawBeenPerformed = await HasDrawBeenPerformedTodayAsync(product.ProductNumber);

            if (!hasDrawBeenPerformed)
            {
                Console.WriteLine($"[WARNING] Draw date for product {product.ProductNumber} has PASSED and NO draw has been performed. Running draw now!");
                return true;
            }
        }

        // 🔹 Om dragningen inte är idag eller har missats, returnera false
        return false;
    }

    private async Task<bool> HasWinnerBeenDeclaredAsync(int productNumber)
    {
        var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fwinner%2Fproduct{productNumber}%2Fwinner.json?alt=media";

        try
        {
            var response = await _httpClient.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Winner already exists for product {productNumber}.");
                return true;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine($"No winner declared yet for product {productNumber}.");
                return false;
            }

            Console.WriteLine($"Error checking winner file for product {productNumber}: {response.StatusCode}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception while checking winner file for product {productNumber}: {ex.Message}");
            return false;
        }
    }

    private void ScheduleDrawTimerForToday(ProductService.ProductData product)
    {
        var currentUtcTime = DateTime.UtcNow;
        var drawUtcTime = product.DrawDate.ToUniversalTime();
        var timeUntilDraw = drawUtcTime - currentUtcTime;

        if (timeUntilDraw <= TimeSpan.Zero)
        {
            Console.WriteLine($"[WARNING] Product {product.ProductNumber}: Draw time has already passed. Skipping timer setup.");
            return;
        }

        // Skapa och starta en timer för tiden kvar
        var timer = new System.Timers.Timer(timeUntilDraw.TotalMilliseconds);
        timer.Elapsed += async (sender, e) => await PerformDrawAsync(product);
        timer.AutoReset = false; // Kör bara en gång
        timer.Start();

        Console.WriteLine($"[INFO] Timer set for product {product.ProductNumber}: Draw will trigger in {timeUntilDraw.TotalMinutes} minutes UTC.");

        _timers[product.ProductNumber] = timer;
    }

    private async Task SaveTimerInfoToFirebase(int productNumber, DateTime currentTime, DateTime drawTime, TimeSpan timeUntilDraw)
    {
        // Skapa JSON-data som innehåller timerinformation
        var timerData = new
        {
            ProductNumber = productNumber,
            CurrentTime = currentTime.ToString("yyyy-MM-dd HH:mm:ss"),
            DrawTime = drawTime.ToString("yyyy-MM-dd HH:mm:ss"),
            TimerLengthInSeconds = (int)timeUntilDraw.TotalSeconds
        };

        // Serialisera data till JSON-format
        var jsonData = JsonSerializer.Serialize(timerData);
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        // Sätt Firebase-sökvägen
        var path = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fconfig%2Fcountedtimerdraw.json";

        // Skicka POST-förfrågan för att spara data till Firebase
        var response = await _httpClient.PostAsync(path, content);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Timer info saved for product {productNumber}.");
        }
        else
        {
            Console.WriteLine($"Failed to save timer info for product {productNumber}. Status code: {response.StatusCode}");
        }
    }

    public async Task CheckAndTriggerDrawAsync()
    {
        Console.WriteLine("Checking timers and triggering draws if applicable...");

        try
        {
            // Hämta timerinformationen från Firebase
            var path = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fconfig%2Fcountedtimerdraw.json?alt=media";
            var response = await _httpClient.GetAsync(path);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to fetch timer info. Status code: {response.StatusCode}");
                return;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var timerInfo = JsonSerializer.Deserialize<List<TimerData>>(jsonResponse);

            if (timerInfo == null || timerInfo.Count == 0)
            {
                Console.WriteLine("No timers found in Firebase.");
                return;
            }

            // Loopa genom alla timers
            foreach (var timer in timerInfo)
            {
                var drawTime = DateTime.Parse(timer.DrawTime);
                var currentTime = DateTime.UtcNow;

                if (currentTime >= drawTime)
                {
                    Console.WriteLine($"Triggering draw for product {timer.ProductNumber} (DrawTime: {drawTime}, CurrentTime: {currentTime}).");

                    // Hämta produkten från ProductService
                    var product = await _productService.GetProductFromFirebaseAsync(timer.ProductNumber);

                    if (product != null)
                    {
                        await PerformDrawAsync(product);
                    }
                    else
                    {
                        Console.WriteLine($"Product {timer.ProductNumber} not found in Firebase. Skipping.");
                    }
                }
                else
                {
                    Console.WriteLine($"Product {timer.ProductNumber}: Not yet time to trigger draw. DrawTime: {drawTime}, CurrentTime: {currentTime}.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in CheckAndTriggerDrawAsync: {ex.Message}");
        }
    }

    private async Task PerformDrawAsync(ProductService.ProductData product)
    {
        try
        {
            Console.WriteLine($"Performing draw for product {product.ProductNumber}: {product.ProductName}");

            // Kontrollera om dragningen redan är utförd idag
            if (await HasDrawBeenPerformedTodayAsync(product.ProductNumber))
            {
                Console.WriteLine($"Draw for product {product.ProductNumber} has already been performed today. Skipping.");
                return;
            }

            // Hämta deltagare för produkten
            var participants = await _drawService.GetAllUserLockInData(product.ProductNumber);

            if (participants.Count == 0)
            {
                Console.WriteLine($"No participants found for product {product.ProductNumber}. Skipping draw.");
                return;
            }

            // Utför dragningen och välj en vinnare
            var winnerEmail = _drawService.DrawWinner(participants);

            Console.WriteLine($"Winner selected for product {product.ProductNumber}: {winnerEmail}");

            // Hämta pris från produkten
            var prize = product.PrizePool;

            // Spara vinnaren i Firebase
            await _drawService.SaveWinnerToFirebase(product.ProductNumber, winnerEmail, prize);

            // Spara tidpunkten för dragningen
            await SaveDrawDateAsync(product.ProductNumber);

            Console.WriteLine($"Winner saved and draw date recorded for product {product.ProductNumber}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error performing draw for product {product.ProductNumber}: {ex.Message}");
        }
        finally
        {
            // Rensa timern efter dragningen
            if (_timers.ContainsKey(product.ProductNumber))
            {
                _timers[product.ProductNumber].Dispose();
                _timers.Remove(product.ProductNumber);
            }
        }
    }

    private async Task<bool> HasDrawBeenPerformedTodayAsync(int productNumber)
    {
        var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2FlastDrawDate.json?alt=media";
        var response = await _httpClient.GetAsync(path);

        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var lastDrawDate = JsonSerializer.Deserialize<DateTime>(jsonResponse).ToUniversalTime();

            Console.WriteLine($"[INFO] Last draw date for product {productNumber}: {lastDrawDate} UTC.");
            Console.WriteLine($"[INFO] Current UTC date: {DateTime.UtcNow.Date}");

            return lastDrawDate.Date == DateTime.UtcNow.Date;
        }

        Console.WriteLine($"[INFO] No previous draw date found for product {productNumber}. Assuming no draw has been performed.");
        return false;
    }

    private async Task SaveDrawDateAsync(int productNumber)
    {
        var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2FlastDrawDate.json";
        var drawDate = DateTime.UtcNow;

        var jsonData = JsonSerializer.Serialize(drawDate);
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(path, content);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Draw date saved for product {productNumber}: {drawDate}");
        }
        else
        {
            Console.WriteLine($"Failed to save draw date for product {productNumber}. Status code: {response.StatusCode}");
        }
    }

    public class TimerData
    {
        public int ProductNumber { get; set; }
        public string CurrentTime { get; set; }
        public string DrawTime { get; set; }
        public int TimerLengthInSeconds { get; set; }
    }
}
