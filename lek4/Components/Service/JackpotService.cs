using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace lek4.Components.Service
{
    public class JackpotService
    {
        private readonly HttpClient _httpClient;
        private readonly UserService _userService;
        private readonly NavigationManager _navigationManager;
        private readonly ILocalStorageService _localStorage;
        public string CurrentUserEmail { get; set; }

        public JackpotService(HttpClient httpClient, UserService userService, NavigationManager navigationManager, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _userService = userService;
            _navigationManager = navigationManager;
            _localStorage = localStorage;
        }

        // Hämta användarens nuvarande krediter från UserService
        public async Task<double> GetUserCredits(string userEmail)
        {
            return await _userService.GetUserCredits();
        }
        public async Task OnLockInClicked()
        {
            // Hämta användarens e-post från localStorage
            var CurrentUserEmail = await _localStorage.GetItemAsync<string>("userEmail");
            int productNumber = 1; // Sätt rätt produktnummer här

            if (!string.IsNullOrEmpty(CurrentUserEmail))
            {
                await JoinJackpot(CurrentUserEmail, productNumber); // Skicka med productNumber
            }
            else
            {
                // Hantera fallet om e-postadressen inte finns i localStorage
                Console.WriteLine("No email found in localStorage.");
            }
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
        public async Task<double> GetCalculatedJackpotAmount()
        {
            // Hämta total credits från Firebase via UserService
            double totalCredits = await _userService.GetTotalCredits();
            // Multiplicera med 0.1 för att få jackpot-beloppet
            return totalCredits * 0.1;
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

        public async Task JoinJackpot(string userEmail, int productNumber = 0)
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
            // Steg 1: Generera en slumpmässig vinnande biljett
            var drawJackpotService = new DrawJackpotService(_httpClient, this);
            var winningTicket = drawJackpotService.GenerateRandomTicket();

            // Steg 2: Hämta alla användarbiljetter från Firebase
            var userTickets = await GetAllTickets(productNumber);
            string winnerEmail = null;

            // Steg 3: Kontrollera om någon användares biljett matchar den genererade biljetten
            foreach (var ticket in userTickets)
            {
                if (drawJackpotService.IsMatchingTicket(ticket, winningTicket))
                {
                    winnerEmail = ticket.UserEmail;
                    break;
                }
            }

            // Steg 4: Spara resultatet till Firebase, oavsett om det finns en vinnare eller inte
            await SaveDrawResult(productNumber, winnerEmail, winningTicket);
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

        private async Task SaveDrawResult(int productNumber, string winnerEmail, JackpotTicket winningTicket)
        {
            var winnerData = new
            {
                Winner = winnerEmail ?? "No winner",  // Spara "No winner" om ingen matchande biljett finns
                DrawDate = DateTime.Now.ToString("yyyy-MM-dd"),
                JackpotAmount = await GetCalculatedJackpotAmount(),
                WinningTicket = winningTicket  // Spara information om den genererade vinnande biljetten
            };

            var winnerJson = JsonSerializer.Serialize(winnerData);
            var content = new StringContent(winnerJson, Encoding.UTF8, "application/json");

            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2Fwinner%2Fproduct{productNumber}%2Fwinner.json";
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

        public async Task UpdateTotalCreditsFromAllUsers()
        {
            try
            {
                double totalCredits = 0;

                // Hämta alla användares filer i mappen UserStats
                var path = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FUserStats?alt=media";
                var response = await _httpClient.GetAsync(path);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var usersData = JsonSerializer.Deserialize<Dictionary<string, UserCreditData>>(responseBody);

                    if (usersData != null)
                    {
                        // Summera credits för alla användare
                        foreach (var user in usersData.Values)
                        {
                            totalCredits += user.Credits;
                        }
                    }
                }

                // Spara den sammanlagda summan av credits i totalcredits.json
                var totalCreditsPath = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FUserStats%2Ftotalcredits.json";
                var totalCreditsJson = JsonSerializer.Serialize(totalCredits);
                var content = new StringContent(totalCreditsJson, Encoding.UTF8, "application/json");

                var putResponse = await _httpClient.PutAsync(totalCreditsPath, content);
                if (!putResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to update totalCredits. Status Code: {putResponse.StatusCode}");
                }
                else
                {
                    Console.WriteLine("Total credits updated successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating total credits: {ex.Message}");
            }
        }
        public async Task<bool> UpdateJackpotTotalLockin(string userEmail, double amount)
        {
            var path = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FJackpot%2FJackpotTotalLockin.json";

            // Hämta den befintliga lock-in-datan
            Dictionary<string, double> jackpotTotalLockin;
            try
            {
                var response = await _httpClient.GetAsync(path + "?alt=media");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    jackpotTotalLockin = JsonSerializer.Deserialize<Dictionary<string, double>>(json) ?? new Dictionary<string, double>();
                }
                else
                {
                    jackpotTotalLockin = new Dictionary<string, double>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching JackpotTotalLockin.json: {ex.Message}");
                jackpotTotalLockin = new Dictionary<string, double>();
            }

            // Uppdatera användarens lock-in utan att påverka jackpotAmount
            jackpotTotalLockin[userEmail] = amount;

            // Spara tillbaka uppdaterad data till Firebase
            var content = new StringContent(JsonSerializer.Serialize(jackpotTotalLockin), Encoding.UTF8, "application/json");
            var putResponse = await _httpClient.PostAsync(path, content);

            if (putResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("JackpotTotalLockin.json updated successfully.");
                return true;
            }
            else
            {
                Console.WriteLine($"Failed to update JackpotTotalLockin.json. Status Code: {putResponse.StatusCode}");
                return false;
            }
        }

        // Hjälpmetod för att hämta nuvarande data från JackpotTotalLockin.json
        private async Task<Dictionary<string, double>> GetCurrentJackpotTotalLockinData()
        {
            var path = "https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FJackpot%2FJackpotTotalLockin.json?alt=media";

            try
            {
                var response = await _httpClient.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<Dictionary<string, double>>(jsonResponse);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine("JackpotTotalLockin.json hittades inte. Skapar en ny.");
                    return null;
                }
                else
                {
                    Console.WriteLine($"Fel vid hämtning av JackpotTotalLockin.json: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ett fel uppstod vid hämtning av JackpotTotalLockin.json: {ex.Message}");
                return null;
            }
        }
        public async Task SaveUserTicketAsSingleLine(int productNumber, string userEmail, string ticketData)
        {
            // Define Firebase path for the ticket
            var path = $"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2FJackpot%2FJackpotConfirmedTickets.json";

            // Retrieve existing data (if any)
            var response = await _httpClient.GetAsync($"{path}?alt=media");
            Dictionary<string, List<string>> existingTickets = new Dictionary<string, List<string>>();

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                existingTickets = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json) ?? new Dictionary<string, List<string>>();
            }

            // Add or update the user's ticket data
            if (!existingTickets.ContainsKey(userEmail))
            {
                existingTickets[userEmail] = new List<string>();
            }
            existingTickets[userEmail].Add(ticketData);

            // Serialize the updated dictionary back to JSON
            var updatedJson = JsonSerializer.Serialize(existingTickets);
            var content = new StringContent(updatedJson, Encoding.UTF8, "application/json");

            // Save back to Firebase
            var putResponse = await _httpClient.PostAsync(path, content);
            if (!putResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to save ticket. Status Code: {putResponse.StatusCode}");
            }
        }
    }
    // Skapa en klass för att hantera dataformatet i JackpotTotalLockin.json
    public class JackpotLockinEntry
    {
        public string UserEmail { get; set; }
        public int Amount { get; set; }
    }
    public class UserCreditData
    {
        public string UserEmail { get; set; }
        public double Credits { get; set; }
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
        public string UserEmail { get; set; } // Lägg till denna rad för att inkludera UserEmail
        public string Number { get; set; }
        public string Color { get; set; }
        public string Symbol { get; set; }
        public string ChineseSymbol { get; set; }
        public string Planet { get; set; }
        public string Element { get; set; }
    }
}
