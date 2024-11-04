using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace lek4.Components.Service
{
    public class JackpotService
    {
        private readonly HttpClient _httpClient;
        private readonly UserService _userService;
        private readonly NavigationManager _navigationManager;

        public JackpotService(HttpClient httpClient, UserService userService, NavigationManager navigationManager)
        {
            _httpClient = httpClient;
            _userService = userService;
            _navigationManager = navigationManager;
        }

        // Hämta användarens nuvarande krediter från UserService
        public async Task<double> GetUserCredits(string userEmail)
        {
            return await _userService.GetUserCredits();
        }

        // Kontrollera om jackpot är aktiv baserat på om jackpot-beloppet är större än 0
        public async Task<bool> IsJackpotActive()
        {
            double jackpotAmount = await GetJackpotAmount();
            return jackpotAmount > 0;
        }
        public async Task AttemptJackpotEntry(string userEmail)
        {
            double userCredits = await GetUserCredits(userEmail);
            const double requiredCredits = 100.0;

            if (userCredits >= requiredCredits)
            {
                // Deduct credits and update user data
                userCredits -= requiredCredits;
                await _userService.UpdateUserCredits(userEmail, (int)userCredits);

                // Redirect to ticket selection page (assumed to be handled by UI)
                Console.WriteLine("User has been redirected to ticket selection.");
            }
            else
            {
                Console.WriteLine("User does not have enough credits to join the jackpot.");
            }
        }
        public async Task SaveUserTicket(int productNumber, string userEmail, JackpotTicket ticket)
        {
            var ticketJson = JsonSerializer.Serialize(ticket);
            var content = new StringContent(ticketJson, Encoding.UTF8, "application/json");

            // Define Firebase path for the ticket
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2Ftickets%2F{userEmail}.json";
            var response = await _httpClient.PutAsync(path, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Ticket saved successfully.");
            }
            else
            {
                Console.WriteLine($"Failed to save ticket. Status Code: {response.StatusCode}");
            }
        }

        // Hämta jackpot-belopp från Firebase Storage
        public async Task<double> GetJackpotAmount()
        {
            try
            {
                var path = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FJackpot%2FjackpotAmount.json?alt=media";
                var response = await _httpClient.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<double>(responseBody);
                }
                else
                {
                    Console.WriteLine($"Failed to retrieve jackpot amount. Status Code: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Request error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
            return 0.0;
        }

        // Hämta deltagare i jackpot från Firebase Storage
        public async Task<List<string>> GetJackpotParticipants()
        {
            var path = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FJackpot%2Fparticipants.json?alt=media";
            var response = await _httpClient.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<string>>(responseBody) ?? new List<string>();
            }
            return new List<string>(); // Returnerar en tom lista om inga deltagare hittas
        }

        // Dra av krediter och låt användaren gå med i jackpot
        public async Task JoinJackpot(string userEmail, int productNumber)
        {
            // Hämta användarens credits
            double userCredits = await GetUserCredits(userEmail);

            if (userCredits < 100)
            {
                Console.WriteLine("Insufficient credits to join the jackpot.");
                return;
            }

            // Dra av credits och uppdatera användarens saldo
            userCredits -= 100;
            await _userService.UpdateUserCredits(userEmail, (int)userCredits);

            // Uppdatera jackpot-beloppet
            double jackpotAmount = await GetJackpotAmount();
            jackpotAmount += 10.0;
            await UpdateJackpotAmount(jackpotAmount);

            // Lägg till användaren i deltagarlistan
            await AddParticipantToJackpot(userEmail);

            Console.WriteLine("Användare har lagts till i jackpotten.");
        }

        // Uppdatera jackpot-belopp i Firebase Storage
        public async Task UpdateJackpotAmount(double newAmount)
        {
            var path = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FJackpot%2FjackpotAmount.json";
            var jackpotJson = JsonSerializer.Serialize(newAmount);
            var content = new StringContent(jackpotJson, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync(path, content);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to update jackpot amount. Status Code: {response.StatusCode}");
            }
        }


        // Lägg till en deltagare till jackpot-deltagarlistan i Firebase Storage
        private async Task AddParticipantToJackpot(string userEmail)
        {
            var participants = await GetJackpotParticipants();
            if (!participants.Contains(userEmail))
            {
                participants.Add(userEmail);
                var path = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FJackpot%2Fparticipants.json";
                var participantsJson = JsonSerializer.Serialize(participants);
                var content = new StringContent(participantsJson, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(path, content);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to add participant to jackpot. Status Code: {response.StatusCode}");
                }
            }
        }
        public async Task DrawJackpotWinner(int productNumber)
        {
            // Step 1: Get all tickets
            var tickets = await GetAllTickets(productNumber);
            if (tickets.Count == 0)
            {
                Console.WriteLine("No tickets available for drawing.");
                return;
            }

            // Step 2: Select a random ticket
            var random = new Random();
            int winningIndex = random.Next(tickets.Count);
            var winningTicket = tickets[winningIndex];

            // Step 3: Save the winning ticket to Firebase
            await SaveDrawResult(productNumber, winningTicket);

            Console.WriteLine("Jackpot winner drawn and saved successfully.");
        }

        private async Task<List<JackpotTicket>> GetAllTickets(int productNumber)
        {
            var tickets = new List<JackpotTicket>();

            try
            {
                // Firebase path to tickets folder
                var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2Ftickets.json?alt=media";
                var response = await _httpClient.GetAsync(path);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    tickets = JsonSerializer.Deserialize<List<JackpotTicket>>(jsonResponse) ?? new List<JackpotTicket>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching tickets: {ex.Message}");
            }

            return tickets;
        }

        private async Task SaveDrawResult(int productNumber, JackpotTicket winningTicket)
        {
            var winningTicketJson = JsonSerializer.Serialize(winningTicket);
            var content = new StringContent(winningTicketJson, Encoding.UTF8, "application/json");

            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fproducts%2Fproduct{productNumber}%2FdrawResult.json";
            var response = await _httpClient.PutAsync(path, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Draw result saved successfully.");
            }
            else
            {
                Console.WriteLine($"Failed to save draw result. Status Code: {response.StatusCode}");
            }
        }

    }

    // Modell för att representera data relaterad till jackpot
    public class JackpotData
    {
        public int JackpotId { get; set; }
        public string UserEmail { get; set; }
        public double PrizePool { get; set; }
        public bool IsJackpot { get; set; } = true;
        public string JackpotName { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<string> Participants { get; set; } = new List<string>();
    }
    public class JackpotTicket
    {
        public string Number { get; set; }
        public string Color { get; set; }
        public string Symbol { get; set; }
        public string Planet { get; set; }
        public string Exoplanet { get; set; }
    }

}
