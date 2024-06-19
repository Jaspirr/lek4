using System;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

namespace lek4.Components.Service
{
    public class EmailAuthService
    {
        private FirebaseAuth auth;

        public EmailAuthService()
        {
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile("path/to/your/serviceAccountKey.json")
            });

            auth = FirebaseAuth.DefaultInstance;
        }

        public async Task<UserRecord> SignUpWithEmailAndPassword(string email, string password)
        {
            try
            {
                var userRecordArgs = new UserRecordArgs()
                {
                    Email = email,
                    Password = password,
                    EmailVerified = false,
                    Disabled = false
                };
                var userRecord = await auth.CreateUserAsync(userRecordArgs);
                return userRecord;
            }
            catch (FirebaseAuthException ex)
            {
                // Hantera fel
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }

        public async Task<UserRecord> SignInWithEmailAndPassword(string email, string password)
        {
            try
            {
                // Firebase Admin SDK stödjer inte direkt autentisering. Du kan använda en anpassad autentiseringstoken här
                var token = await auth.CreateCustomTokenAsync(email);
                var userRecord = await auth.GetUserByEmailAsync(email);
                return userRecord;
            }
            catch (FirebaseAuthException ex)
            {
                // Hantera fel
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }

        public async Task SignOut()
        {
            // Det finns ingen direkt sign-out metod i Firebase Admin SDK.
            // Du kan hantera tokenbaserad utloggning i klienten.
            await Task.CompletedTask;
        }

        public async Task<UserRecord> GetCurrentUser(string uid)
        {
            try
            {
                var userRecord = await auth.GetUserAsync(uid);
                return userRecord;
            }
            catch (FirebaseAuthException ex)
            {
                // Hantera fel
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }
    }
}
