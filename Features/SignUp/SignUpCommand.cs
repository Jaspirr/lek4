using lek4.Components.Commands;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace lek4.Features.SignUp
{
    public class SignUpCommand : AsyncCommandBase
    {
        private readonly SignUpFormViewModel _viewModel;
        private static readonly HttpClient httpClient = new HttpClient();

        public SignUpCommand(SignUpFormViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        protected override async Task ExecuteAsync(object parameter)
        {
            // Kontrollera att lösenord matchar
            if (_viewModel.Password != _viewModel.ConfirmPassword)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Password and Confirm password do not match!", "Ok");
                return;
            }

            try
            {
                // Firebase API-nyckel
                var apiKey = "AIzaSyCyLKylikL5dUKQEKxMn6EkY6PnBWKmJtA"; // Replace with your Firebase API key
                var requestUri = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={apiKey}";

                // Skapa JSON-payload för registreringsanrop
                var payload = new
                {
                    email = _viewModel.Email,
                    password = _viewModel.Password,
                    returnSecureToken = true
                };

                // Serialisera payload till JSON
                var jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Skicka POST-förfrågan till Firebase för att skapa användare
                var response = await httpClient.PostAsync(requestUri, content);
                response.EnsureSuccessStatusCode(); // Kontrollera om anropet lyckades

                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response from Firebase: {responseBody}");

                // Läs in Firebase-svaret som JSON
                var responseJson = JsonConvert.DeserializeObject<dynamic>(responseBody);
                var idToken = responseJson.idToken.ToString();
                var localId = responseJson.localId.ToString(); // Firebase UID för användaren

                // Skicka e-postverifiering
                await SendEmailVerificationAsync(idToken);


                // Spara användarprofil i Firebase Storage
                await SaveUserProfileToStorage(localId, _viewModel.FirstName, _viewModel.LastName);

                // Visa framgångsmeddelande
                await Application.Current.MainPage.DisplayAlert("Success", "Successfully signed up! Please verify your email address.", "Ok");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to sign up: {ex.Message}", "Ok");
            }
        }

        // Metod för att skicka e-postverifiering
        private async Task SendEmailVerificationAsync(string idToken)
        {
            var apiKey = "AIzaSyCyLKylikL5dUKQEKxMn6EkY6PnBWKmJtA"; // Replace with your Firebase API key
            var requestUri = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={apiKey}";

            var payload = new
            {
                requestType = "VERIFY_EMAIL",
                idToken = idToken
            };

            // Serialisera payload för e-postverifiering
            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Skicka POST-förfrågan till Firebase för att skicka verifieringsmejl
            var response = await httpClient.PostAsync(requestUri, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response from email verification: {responseBody}");
            response.EnsureSuccessStatusCode(); // Kontrollera om anropet lyckades
        }

        // Metod för att kontrollera om användaren är admin
       

        // Metod för att spara användarens profil i Firebase Storage
        private async Task SaveUserProfileToStorage(string userId, string firstName, string lastName)
        {
            var profileData = new
            {
                Email = _viewModel.Email,
                FirstName = firstName,
                LastName = lastName,
            };

            // Serialisera användarens profil till JSON
            var jsonPayload = JsonConvert.SerializeObject(profileData);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Skicka PUT-förfrågan till Firebase Storage för att spara användarprofilen
            var response = await httpClient.PutAsync($"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2F{userId}.json", content);
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response from saving user profile: {responseBody}");

            // Kontrollera om anropet lyckades
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to save user profile: {responseBody}");
            }
        }
    }
}
