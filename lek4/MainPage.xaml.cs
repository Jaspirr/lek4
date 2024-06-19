using lek4.Components.Service;

namespace lek4
{
    public partial class MainPage : ContentPage
    {
        private EmailAuthService authService;

        public MainPage()
        {
            InitializeComponent();
            authService = new EmailAuthService();
        }
        private async void OnSignUpClicked(object sender, EventArgs e)
        {
            var email = EmailEntry.Text;
            var password = PasswordEntry.Text;

            var user = await authService.SignUpWithEmailAndPassword(email, password);

            if (user != null)
            {
                MessageLabel.Text = $"Sign Up Successful: {user.Email}";
            }
            else
            {
                MessageLabel.Text = "Sign Up Failed";
            }
        }

        private async void OnSignInClicked(object sender, EventArgs e)
        {
            var email = EmailEntry.Text;
            var password = PasswordEntry.Text;

            var user = await authService.SignInWithEmailAndPassword(email, password);

            if (user != null)
            {
                MessageLabel.Text = $"Sign In Successful: {user.Email}";
            }
            else
            {
                MessageLabel.Text = "Sign In Failed";
            }
        }
    }
}
