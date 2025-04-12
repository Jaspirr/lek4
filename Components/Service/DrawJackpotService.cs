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

            if (!colors.Any() || !symbols.Any() || !chineseSymbols.Any() || !planets.Any() || !elements.Any())
            {
                Console.WriteLine("One or more categories are empty. Cannot generate a random ticket.");
                return null; // Eller kasta ett undantag om detta är en oacceptabel situation
            }

            return new JackpotTicket
            {
                Number = random.Next(1, 51).ToString(),
                Color = colors[random.Next(colors.Count)].Name,
                Symbol = symbols[random.Next(symbols.Count)].Name,
                ChineseSymbol = chineseSymbols[random.Next(chineseSymbols.Count)].Name,
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
            // Generera en slumpmässig biljett
            var winningTicket = GenerateRandomTicket();
            var userTickets = await GetJackpotTickets();
            string winnerEmail = null;

            // Sök efter en matchande biljett
            foreach (var ticket in userTickets)
            {
                if (IsMatchingTicket(ticket, winningTicket))
                {
                    winnerEmail = ticket.UserEmail;
                    break;
                }
            }

            // Spara resultatet i Firebase, oavsett om det finns en vinnare eller inte
            await SaveWinnerToFirebase(productNumber, winnerEmail, DateTime.Now, winningTicket);

            if (winnerEmail == null)
            {
                Console.WriteLine("No matching ticket found. Saving random ticket as no-winner result.");
            }
            else
            {
                Console.WriteLine($"Winner found: {winnerEmail}. Saving to Firebase.");
            }
        }

        private List<ColorOption> colors = new List<ColorOption>
        {
            new ColorOption { Name = "Red", ColorCode = "#FF5733" },
            new ColorOption { Name = "Green", ColorCode = "#33FF57" },
            new ColorOption { Name = "Blue", ColorCode = "#3357FF" },
            new ColorOption { Name = "Yellow", ColorCode = "#FFFF33" },
            new ColorOption { Name = "Purple", ColorCode = "#9B33FF" },
            new ColorOption { Name = "Orange", ColorCode = "#FF8C33" },
            new ColorOption { Name = "Pink", ColorCode = "#FF33A8" },
            new ColorOption { Name = "Brown", ColorCode = "#8B4513" },
            new ColorOption { Name = "Cyan", ColorCode = "#33FFF6" },
            new ColorOption { Name = "Magenta", ColorCode = "#FF33FF" },
            new ColorOption { Name = "Teal", ColorCode = "#008080" },
            new ColorOption { Name = "Lime", ColorCode = "#BFFF00" },
            new ColorOption { Name = "Olive", ColorCode = "#808000" },
            new ColorOption { Name = "Maroon", ColorCode = "#800000" },
            new ColorOption { Name = "Navy", ColorCode = "#000080" },
            new ColorOption { Name = "Gold", ColorCode = "#FFD700" },
            new ColorOption { Name = "Silver", ColorCode = "#C0C0C0" },
            new ColorOption { Name = "Coral", ColorCode = "#FF7F50" },
            new ColorOption { Name = "Turquoise", ColorCode = "#40E0D0" },
            new ColorOption { Name = "Indigo", ColorCode = "#4B0082" },
            new ColorOption { Name = "Lavender", ColorCode = "#E6E6FA" },
            new ColorOption { Name = "Salmon", ColorCode = "#FA8072" },
            new ColorOption { Name = "SlateBlue", ColorCode = "#6A5ACD" },
            new ColorOption { Name = "Mint", ColorCode = "#98FF98" },
            new ColorOption { Name = "Crimson", ColorCode = "#DC143C" },
            new ColorOption { Name = "Peach", ColorCode = "#FFE5B4" },
            new ColorOption { Name = "ForestGreen", ColorCode = "#228B22" },
            new ColorOption { Name = "SkyBlue", ColorCode = "#87CEEB" },
            new ColorOption { Name = "Violet", ColorCode = "#EE82EE" },
            new ColorOption { Name = "Tan", ColorCode = "#D2B48C" },
            new ColorOption { Name = "Sienna", ColorCode = "#A0522D" },
            new ColorOption { Name = "Khaki", ColorCode = "#F0E68C" },
            new ColorOption { Name = "Orchid", ColorCode = "#DA70D6" },
            new ColorOption { Name = "Chocolate", ColorCode = "#D2691E" },
            new ColorOption { Name = "Tomato", ColorCode = "#FF6347" },
            new ColorOption { Name = "SteelBlue", ColorCode = "#4682B4" },
            new ColorOption { Name = "Periwinkle", ColorCode = "#CCCCFF" },
            new ColorOption { Name = "Lemon", ColorCode = "#FFF44F" },
            new ColorOption { Name = "SeaGreen", ColorCode = "#2E8B57" },
            new ColorOption { Name = "Fuchsia", ColorCode = "#FF00FF" },
            new ColorOption { Name = "Aqua", ColorCode = "#00FFFF" },
            new ColorOption { Name = "Amethyst", ColorCode = "#9966CC" },
            new ColorOption { Name = "Copper", ColorCode = "#B87333" },
            new ColorOption { Name = "Ruby", ColorCode = "#E0115F" },
            new ColorOption { Name = "Emerald", ColorCode = "#50C878" },
            new ColorOption { Name = "Charcoal", ColorCode = "#36454F" },
            new ColorOption { Name = "Ivory", ColorCode = "#FFFFF0" },
            new ColorOption { Name = "Sand", ColorCode = "#C2B280" },
            new ColorOption { Name = "MidnightBlue", ColorCode = "#191970" },
            new ColorOption { Name = "Rose", ColorCode = "#FF007F" }
        };

            private List<SymbolOption> symbols = new List<SymbolOption>
        {
            new SymbolOption { Name = "Anchor", IconClass = "fas fa-anchor" },
            new SymbolOption { Name = "Frog", IconClass = "fas fa-frog" },
            new SymbolOption { Name = "Balance Scale", IconClass = "fas fa-balance-scale" },
            new SymbolOption { Name = "Bell", IconClass = "fas fa-bell" },
            new SymbolOption { Name = "Bicycle", IconClass = "fas fa-bicycle" },
            new SymbolOption { Name = "Binoculars", IconClass = "fas fa-binoculars" },
            new SymbolOption { Name = "Bolt", IconClass = "fas fa-bolt" },
            new SymbolOption { Name = "Bomb", IconClass = "fas fa-bomb" },
            new SymbolOption { Name = "Book", IconClass = "fas fa-book" },
            new SymbolOption { Name = "Briefcase", IconClass = "fas fa-briefcase" },
            new SymbolOption { Name = "Camera", IconClass = "fas fa-camera" },
            new SymbolOption { Name = "Car", IconClass = "fas fa-car" },
            new SymbolOption { Name = "Certificate", IconClass = "fas fa-certificate" },
            new SymbolOption { Name = "Cloud", IconClass = "fas fa-cloud" },
            new SymbolOption { Name = "Code", IconClass = "fas fa-code" },
            new SymbolOption { Name = "Coffee", IconClass = "fas fa-coffee" },
            new SymbolOption { Name = "Compass", IconClass = "fas fa-compass" },
            new SymbolOption { Name = "Crown", IconClass = "fas fa-crown" },
            new SymbolOption { Name = "Dice", IconClass = "fas fa-dice" },
            new SymbolOption { Name = "Dragon", IconClass = "fas fa-dragon" },
            new SymbolOption { Name = "Feather", IconClass = "fas fa-feather" },
            new SymbolOption { Name = "Fire", IconClass = "fas fa-fire" },
            new SymbolOption { Name = "Football", IconClass = "fas fa-football-ball" },
            new SymbolOption { Name = "Ghost", IconClass = "fas fa-ghost" },
            new SymbolOption { Name = "Globe", IconClass = "fas fa-globe" },
            new SymbolOption { Name = "Guitar", IconClass = "fas fa-guitar" },
            new SymbolOption { Name = "Heart", IconClass = "fas fa-heart" },
            new SymbolOption { Name = "Horse", IconClass = "fas fa-horse" },
            new SymbolOption { Name = "Key", IconClass = "fas fa-key" },
            new SymbolOption { Name = "Leaf", IconClass = "fas fa-leaf" },
            new SymbolOption { Name = "Lightbulb", IconClass = "fas fa-lightbulb" },
            new SymbolOption { Name = "Magic", IconClass = "fas fa-magic" },
            new SymbolOption { Name = "Medal", IconClass = "fas fa-medal" },
            new SymbolOption { Name = "Mobile", IconClass = "fas fa-mobile-alt" },
            new SymbolOption { Name = "Moon", IconClass = "fas fa-moon" },
            new SymbolOption { Name = "Music", IconClass = "fas fa-music" },
            new SymbolOption { Name = "Paper Plane", IconClass = "fas fa-paper-plane" },
            new SymbolOption { Name = "Paw", IconClass = "fas fa-paw" },
            new SymbolOption { Name = "Plane", IconClass = "fas fa-plane" },
            new SymbolOption { Name = "Robot", IconClass = "fas fa-robot" },
            new SymbolOption { Name = "Rocket", IconClass = "fas fa-rocket" },
            new SymbolOption { Name = "Skull", IconClass = "fas fa-skull" },
            new SymbolOption { Name = "Snowflake", IconClass = "fas fa-snowflake" },
            new SymbolOption { Name = "Star", IconClass = "fas fa-star" },
            new SymbolOption { Name = "Sun", IconClass = "fas fa-sun" },
            new SymbolOption { Name = "Theater Masks", IconClass = "fas fa-theater-masks" },
            new SymbolOption { Name = "Thumbs Up", IconClass = "fas fa-thumbs-up" },
            new SymbolOption { Name = "Tree", IconClass = "fas fa-tree" },
            new SymbolOption { Name = "Umbrella", IconClass = "fas fa-umbrella" },
            new SymbolOption { Name = "Volleyball", IconClass = "fas fa-volleyball-ball" }
        };

            private List<ChineseSymbolOption> chineseSymbols = new List<ChineseSymbolOption> {  

            new ChineseSymbolOption { Name = "Orion", ImageUrl = "images/Orion.webp" },
            new ChineseSymbolOption { Name = "Andromeda", ImageUrl = "images/Andromeda.webp" },
            new ChineseSymbolOption { Name = "Leo", ImageUrl = "images/Lejonet.webp" },
            new ChineseSymbolOption { Name = "Sagittarius", ImageUrl = "images/Skytten.webp" },
            new ChineseSymbolOption { Name = "Scorpius", ImageUrl = "images/Skorpionen.webp" },
            new ChineseSymbolOption { Name = "Aries", ImageUrl = "images/Vaduren.webp" },
            new ChineseSymbolOption { Name = "Gemini", ImageUrl = "images/Tvillingarna.webp" },
            new ChineseSymbolOption { Name = "Taurus", ImageUrl = "images/Oxen.webp" },
            new ChineseSymbolOption { Name = "Libra", ImageUrl = "images/Vagen.webp" },
            new ChineseSymbolOption { Name = "Virgo", ImageUrl = "images/Jungfrun.webp" },
            new ChineseSymbolOption { Name = "Cancer", ImageUrl = "images/Kraftan.webp" },
            new ChineseSymbolOption { Name = "Ursa Major", ImageUrl = "images/Storabjornen.webp" },
            new ChineseSymbolOption { Name = "Capricornus", ImageUrl = "images/Stenbocken.webp" },
            new ChineseSymbolOption { Name = "Pisces", ImageUrl = "images/Fiskarna.webp" },
            new ChineseSymbolOption { Name = "Draco", ImageUrl = "images/Draken.webp" },
            new ChineseSymbolOption { Name = "Aquila", ImageUrl = "images/Ornen.webp" },
            new ChineseSymbolOption { Name = "Centaurus", ImageUrl = "images/Kentauren.webp" },
            new ChineseSymbolOption { Name = "Lyra", ImageUrl = "images/Lyra.webp" },
            new ChineseSymbolOption { Name = "Canis Major", ImageUrl = "images/Orionhunden.webp" },
            new ChineseSymbolOption { Name = "Delphinus", ImageUrl = "images/Delfinen.webp" },
            new ChineseSymbolOption { Name = "Corvus", ImageUrl = "images/Korpen.webp" },
            new ChineseSymbolOption { Name = "Crater", ImageUrl = "images/Krukan.webp" },
            new ChineseSymbolOption { Name = "Hydra", ImageUrl = "images/Hydran.webp" },
            new ChineseSymbolOption { Name = "Chamaeleon", ImageUrl = "images/Kameleonten.webp" },
            new ChineseSymbolOption { Name = "Telescopium", ImageUrl = "images/Teleskopet.webp" },
            new ChineseSymbolOption { Name = "Dorado", ImageUrl = "images/Svardfisken.webp" },
            new ChineseSymbolOption { Name = "Pavo", ImageUrl = "images/Pafageln.webp" },
            new ChineseSymbolOption { Name = "Phoenix", ImageUrl = "images/Fenix.webp" },
            new ChineseSymbolOption { Name = "Volans", ImageUrl = "images/Flygfisken.webp" },
            new ChineseSymbolOption { Name = "Lupus", ImageUrl = "images/Vargen.webp" },
            new ChineseSymbolOption { Name = "Monoceros", ImageUrl = "images/Enhorningen.webp" },
            new ChineseSymbolOption { Name = "Lepus", ImageUrl = "images/Kaninen.webp" }, 
            };

            private List<PlanetOption> planets = new List<PlanetOption>
        {
             new PlanetOption { Name = "Mercury", ImageUrl = "images/mercurus.webp" },
            new PlanetOption { Name = "Venus", ImageUrl = "images/Venus.webp" },
            new PlanetOption { Name = "Earth", ImageUrl = "images/earth.webp" },
            new PlanetOption { Name = "Mars", ImageUrl = "images/Mars.webp" },
            new PlanetOption { Name = "Jupiter", ImageUrl = "images/jupiter.webp" },
            new PlanetOption { Name = "Saturn", ImageUrl = "images/Saturnus.webp" },
            new PlanetOption { Name = "Uranus", ImageUrl = "images/uranus.webp" },
            new PlanetOption { Name = "Neptune", ImageUrl = "images/Neptunus.webp" }
        };

            private List<ElementOption> elements = new List<ElementOption>
        {
                new ElementOption { Name = "Earth", ImageUrl = "/images/elements/Jord.webp" },
            new ElementOption { Name = "Fire", ImageUrl = "/images/elements/Fire.webp" },
            new ElementOption { Name = "Water", ImageUrl = "/images/elements/Water.webp" },
            new ElementOption { Name = "Air", ImageUrl = "/images/elements/Air.webp" },
        };
    }
}
