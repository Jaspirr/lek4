using Firebase.Auth;
using Firebase.Auth.Providers;
using Microsoft.Extensions.DependencyInjection;

public static class FirebaseAuthClientConfiguration
{
    public static void AddFirebaseAuth(this IServiceCollection services, string apiKey)
    {
        var config = new FirebaseAuthConfig
        {
            ApiKey = apiKey,
            AuthDomain = $"{apiKey}.firebaseapp.com",
            Providers = new FirebaseAuthProvider[]
            {
                new EmailProvider()
            }
        };

        var client = new FirebaseAuthClient(config);
        services.AddSingleton(client);
    }
}
