using System;
using System.Windows.Input;

namespace lek4.Features.SignUp
{
    public class SignUpFormViewModel : Page
    {
        private string _email;
        private string _password;
        private string _confirmPassword;

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

        public string ConfirmPassword
        {
            get { return _confirmPassword; }
            set { _confirmPassword = value; OnPropertyChanged(nameof(ConfirmPassword)); }
        }

        public ICommand SignUpCommand { get; }

        public SignUpFormViewModel()
        {
            SignUpCommand = new SignUpCommand(this);
        }
    }
}