using System.Windows.Input;
using Microsoft.AspNetCore.Components;

namespace lek4.Features.SignUp
{
    public class LoginFormViewModel : Page
    {
        private string _email;
        private string _password;
        private readonly NavigationManager _navigationManager;

        public string Email
        {
            get { return _email; }
            set
            {
                _email = value;
                OnPropertyChanged(nameof(Email));
            }
        }

        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        public ICommand LoginCommand { get; }

        public LoginFormViewModel(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
            LoginCommand = new LoginCommand(this, navigationManager);
        }
    }
}
