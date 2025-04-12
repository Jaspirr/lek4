using System.Windows.Input;
using Microsoft.AspNetCore.Components;

namespace lek4.Features.AgeVerification
{
    public class AgeVerificationViewModel : Page
    {
        private bool _isOver18;
        private readonly NavigationManager _navigationManager;

        public bool IsOver18
        {
            get { return _isOver18; }
            set
            {
                _isOver18 = value;
                OnPropertyChanged(nameof(IsOver18));
            }
        }

        public ICommand AgeVerificationCommand { get; }

        public AgeVerificationViewModel(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
            AgeVerificationCommand = new AgeVerificationCommand(this, navigationManager);
        }
    }
}
