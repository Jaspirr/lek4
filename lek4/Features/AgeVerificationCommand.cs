using lek4.Components.Commands;
using Microsoft.AspNetCore.Components;
using Microsoft.Maui.Controls;

namespace lek4.Features.AgeVerification
{
    public class AgeVerificationCommand : AsyncCommandBase
    {
        private readonly AgeVerificationViewModel _viewModel;
        private readonly NavigationManager _navigationManager;

        public AgeVerificationCommand(AgeVerificationViewModel viewModel, NavigationManager navigationManager)
        {
            _viewModel = viewModel;
            _navigationManager = navigationManager;
        }

        protected override async Task ExecuteAsync(object parameter)
        {
            if (_viewModel.IsOver18)
            {
                await SecureStorage.SetAsync("isOver18", "true");
                _navigationManager.NavigateTo("/login");
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Access Denied", "You must be over 18 to use this app.", "Ok");
            }
        }
    }
}
