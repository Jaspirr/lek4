using lek4.Components.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace lek4.Features.SignUp
{
    public class LoginCommand : AsyncCommandBase
    {
        private readonly LoginFormViewModel _viewModel;
        private static readonly HttpClient httpClient = new HttpClient();

        public LoginCommand(LoginFormViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        protected override async Task ExecuteAsync(object parameter)
        {
            try
            {
                var apiKey = "AIzaSyCyLKylikL5dUKQEKxMn6EkY6PnBWKmJtA"; // Replace with your Firebase API key
                var requestUri = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";

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
                var emailVerified = await IsEmailVerifiedAsync(idToken);

                if (!emailVerified)
                {
                    await Application.Current.MainPage.DisplayAlert("Verification Required", "Please verify your email address before logging in.", "Ok");
                    return;
                }

                await Application.Current.MainPage.DisplayAlert("Success", "Successfully logged in!", "Ok");
                // Navigate to the next page or perform other actions after successful login
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to log in: {ex.Message}", "Ok");
            }
        }

        private async Task<bool> IsEmailVerifiedAsync(string idToken)
        {
            var apiKey = "AIzaSyCyLKylikL5dUKQEKxMn6EkY6PnBWKmJtA"; // Replace with your Firebase API key
            var requestUri = $"https://identitytoolkit.googleapis.com/v1/accounts:lookup?key={apiKey}";

            var payload = new
            {
                idToken = idToken
            };

            var content = new StringContent(JObject.FromObject(payload).ToString(), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(requestUri, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var responseJson = JObject.Parse(responseBody);
            var users = responseJson["users"] as JArray;

            if (users != null && users.Count > 0)
            {
                return (bool)users[0]["emailVerified"];
            }

            return false;
        }
    }
}
