using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Blazored.LocalStorage;

namespace lek4.Components.Service
{
    public class JackpotService
    {
        private readonly HttpClient _httpClient;
        private readonly UserService _userService;

        public JackpotService(HttpClient httpClient, UserService userService)
        {
            _httpClient = httpClient;
            _userService = userService;
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
        public async Task JoinJackpot(string userEmail)
        {
            // Dra av 100 credits från användaren
            double userCredits = await GetUserCredits(userEmail);
            if (userCredits < 100)
            {
                Console.WriteLine("Insufficient credits to join the jackpot.");
                return;
            }
            userCredits -= 100;
            await _userService.UpdateUserCredits(userEmail, userCredits);

            // Uppdatera jackpot-beloppet
            double jackpotAmount = await GetJackpotAmount();
            jackpotAmount += 10.0; // Anta att varje deltagare bidrar med 10 kr
            await UpdateJackpotAmount(jackpotAmount);

            // Lägg till användaren till deltagarlistan
            await AddParticipantToJackpot(userEmail);
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
}
