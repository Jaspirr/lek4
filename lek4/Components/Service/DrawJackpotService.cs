using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using lek4.Components.Service;
using static lek4.Components.Pages.SelectTicket; // Refererar till den gemensamma JackpotTicket-klassen

namespace lek4.Components.Service
{
    public class DrawJackpotService
    {
        private readonly HttpClient _httpClient;
        private readonly JackpotService _jackpotService;
        private const string JackpotTicketsPath = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FJackpot%2FJackpotConfirmedTickets.json?alt=media";

        private List<ColorOption> colors = new List<ColorOption> { /* Lägg till färgalternativ här */ };
        private List<SymbolOption> symbols = new List<SymbolOption> { /* Lägg till symboler här */ };
        private List<string> chineseSymbols = new List<string> { /* Lägg till kinesiska symboler här */ };
        private List<PlanetOption> planets = new List<PlanetOption> { /* Lägg till planeter här */ };
        private List<ElementOption> elements = new List<ElementOption> { /* Lägg till element här */ };

        public DrawJackpotService(HttpClient httpClient, JackpotService jackpotService)
        {
            _httpClient = httpClient;
            _jackpotService = jackpotService;
        }

        public async Task<List<JackpotTicket>> GetJackpotTickets()
        {
            var response = await _httpClient.GetAsync(JackpotTicketsPath);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Failed to fetch jackpot tickets.");
                return new List<JackpotTicket>();
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            try
            {
                var ticketsDictionary = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonResponse);
                var tickets = ticketsDictionary?.SelectMany(kvp => kvp.Value.Select(ticketString =>
                {
                    var parts = ticketString.Split(", ");
                    return new JackpotTicket
                    {
                        Number = parts[0],
                        Color = parts[1],
                        Symbol = parts[2],
                        ChineseSymbol = parts[3],
                        Planet = parts[4],
                        Element = parts[5]
                    };
                })).ToList() ?? new List<JackpotTicket>();

                return tickets;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error deserializing jackpot tickets: {ex.Message}");
                return new List<JackpotTicket>();
            }
        }

        public JackpotTicket GenerateRandomTicket()
        {
            var random = new Random();

            return new JackpotTicket
            {
                Number = random.Next(1, 51).ToString(),
                Color = colors[random.Next(colors.Count)].Name,
                Symbol = symbols[random.Next(symbols.Count)].Name,
                ChineseSymbol = chineseSymbols[random.Next(chineseSymbols.Count)],
                Planet = planets[random.Next(planets.Count)].Name,
                Element = elements[random.Next(elements.Count)].Name
            };
        }

        public bool IsMatchingTicket(JackpotTicket ticket1, JackpotTicket ticket2)
        {
            return ticket1.Number == ticket2.Number &&
                   ticket1.Color == ticket2.Color &&
                   ticket1.Symbol == ticket2.Symbol &&
                   ticket1.ChineseSymbol == ticket2.ChineseSymbol &&
                   ticket1.Planet == ticket2.Planet &&
                   ticket1.Element == ticket2.Element;
        }

        public async Task SaveWinnerToFirebase(int productNumber, string winnerEmail, DateTime drawDate, JackpotTicket winningTicket)
        {
            var winnerData = new
            {
                Winner = winnerEmail ?? "No winner",
                DrawDate = drawDate.ToString("yyyy-MM-dd"),
                JackpotAmount = await _jackpotService.GetCalculatedJackpotAmount(),
                WinningTicket = winningTicket
            };

            var winnerJson = JsonSerializer.Serialize(winnerData);
            var content = new StringContent(winnerJson, Encoding.UTF8, "application/json");

            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fwinner%2Fproduct{productNumber}%2Fwinner.json";
            var response = await _httpClient.PostAsync(path, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Winner data saved successfully for product {productNumber}.");
            }
            else
            {
                Console.WriteLine($"Failed to save winner data for product {productNumber}. Error: {response.StatusCode}");
            }
        }
        public JackpotTicket DrawRandomWinner(List<JackpotTicket> tickets)
        {
            if (tickets == null || tickets.Count == 0)
            {
                Console.WriteLine("No tickets available for drawing.");
                return null;
            }

            var random = new Random();
            int winnerIndex = random.Next(tickets.Count);
            return tickets[winnerIndex];
        }

        public async Task DrawAndSaveJackpotWinner(int productNumber)
        {
            var winningTicket = GenerateRandomTicket();
            var userTickets = await GetJackpotTickets();
            string winnerEmail = null;

            foreach (var ticket in userTickets)
            {
                if (IsMatchingTicket(ticket, winningTicket))
                {
                    winnerEmail = ticket.UserEmail;
                    break;
                }
            }

            if (winnerEmail != null)
            {
                await SaveWinnerToFirebase(productNumber, winnerEmail, DateTime.Now, winningTicket);
            }
            else
            {
                Console.WriteLine("No matching ticket found.");
            }
        }
    }
}
