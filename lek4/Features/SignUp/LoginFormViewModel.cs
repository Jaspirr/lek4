using System;
using System.Windows.Input;

namespace lek4.Features.SignUp
{
    public class LoginFormViewModel : Page
    {
        private string _email;
        private string _password;

        public string Email
        {
            get { return _email; }
            set { _email = value; OnPropertyChanged(nameof(Email)); }
        }

        public string Password
        {
            get { return _password; }
            set { _password = value; OnPropertyChanged(nameof(Password)); }
        }

        public ICommand LoginCommand { get; }

        public LoginFormViewModel()
        {
            LoginCommand = new LoginCommand(this);
        }
    }
}
