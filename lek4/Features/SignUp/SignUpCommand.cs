using lek4.Components.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            if (_viewModel.Password != _viewModel.ConfirmPassword)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Password and Confirm password do not match!", "Ok");
                return;
            }

            try
            {
                var apiKey = "AIzaSyCyLKylikL5dUKQEKxMn6EkY6PnBWKmJtA"; // Replace with your Firebase API key
                var requestUri = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={apiKey}";

                var payload = new
                {
                    email = _viewModel.Email,
                    password = _viewModel.Password,
                    returnSecureToken = true
                };

                var content = new StringContent(JObject.FromObject(payload).ToString(), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(requestUri, content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var responseJson = JObject.Parse(responseBody);
                var idToken = responseJson["idToken"].ToString();
                var localId = responseJson["localId"].ToString(); // Firebase UID för användaren

                // Skicka email-verifiering
                await SendEmailVerificationAsync(idToken);

                // Kontrollera om användaren är admin
                bool isAdmin = IsAdminEmail(_viewModel.Email);

                // Spara användarprofilen
                await SaveUserProfileToStorage(localId, _viewModel.FirstName, _viewModel.LastName, isAdmin);

                await Application.Current.MainPage.DisplayAlert("Success", "Successfully signed up! Please verify your email address.", "Ok");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to sign up: {ex.Message}", "Ok");
            }
        }

        private async Task SendEmailVerificationAsync(string idToken)
        {
            var apiKey = "AIzaSyCyLKylikL5dUKQEKxMn6EkY6PnBWKmJtA"; // Replace with your Firebase API key
            var requestUri = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={apiKey}";

            var payload = new
            {
                requestType = "VERIFY_EMAIL",
                idToken = idToken
            };

            var content = new StringContent(JObject.FromObject(payload).ToString(), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(requestUri, content);
            response.EnsureSuccessStatusCode();
        }

        private bool IsAdminEmail(string email)
        {
            // Check if the user is admin based on their email address
            var adminEmails = new List<string> { "jesper.erlandsson@hotmail.com" }; // Add more if necessary
            return adminEmails.Contains(email.ToLower());
        }

        private async Task SaveUserProfileToStorage(string userId, string firstName, string lastName, bool isAdmin)
        {
            var profileData = new
            {
                Email = _viewModel.Email,
                FirstName = firstName, // Ta emot förnamnet
                LastName = lastName,   // Ta emot efternamnet
                isAdmin = isAdmin
            };

            var json = JsonConvert.SerializeObject(profileData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PutAsync($"https://firebasestorage.googleapis.com/v0/b/stega-426008.appspot.com/o/users%2F{userId}.json", content); // Replace with your Firebase Storage URL

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to save user profile: {responseBody}");
            }
        }
    }
}
