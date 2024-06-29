using lek4.Components.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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

                // Send email verification
                await SendEmailVerificationAsync(idToken);

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
    }
}