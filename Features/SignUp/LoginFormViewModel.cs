using System.Windows.Input;
using Microsoft.AspNetCore.Components;
using Firebase.Auth;

namespace lek4.Features.SignUp
{
    public class LoginFormViewModel : Page
    {
        private string _email;
        private string _password;
        private readonly NavigationManager _navigationManager;
        private readonly FirebaseAuthClient _authClient;

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

        public LoginFormViewModel(NavigationManager navigationManager, FirebaseAuthClient authClient)
        {
            _navigationManager = navigationManager;
            _authClient = authClient;
            LoginCommand = new LoginCommand(this, navigationManager, authClient);
        }
    }
}
