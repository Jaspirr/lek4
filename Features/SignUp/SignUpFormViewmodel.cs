using System;
using System.ComponentModel;
using System.Windows.Input;

namespace lek4.Features.SignUp
{
    public class SignUpFormViewModel : INotifyPropertyChanged
    {
        private string _email;
        private string _password;
        private string _confirmPassword;

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string Email
        {
            get { return _email; }
            set
            {
                if (_email != value)
                {
                    _email = value;
                    OnPropertyChanged(nameof(Email));
                }
            }
        }

        public string Password
        {
            get { return _password; }
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged(nameof(Password));
                }
            }
        }

        public string ConfirmPassword
        {
            get { return _confirmPassword; }
            set
            {
                if (_confirmPassword != value)
                {
                    _confirmPassword = value;
                    OnPropertyChanged(nameof(ConfirmPassword));
                }
            }
        }

        // Kommando för att trigga registrering
        public ICommand SignUpCommand { get; }

        public SignUpFormViewModel()
        {
            SignUpCommand = new SignUpCommand(this);
        }

        // INotifyPropertyChanged-implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
