using Firebase.Auth;
using Firebase.Auth.Providers;
using lek4.Components.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace lek4
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();

            builder.Services.AddSingleton<GoogleFitService>();
            builder.Services.AddScoped<AdService>();

            // Register NumberService
            builder.Services.AddSingleton<Components.Service.NumberService>();
            builder.Services.AddFirebaseAuth("AIzaSyCyLKylikL5dUKQEKxMn6EkY6PnBWKmJtA");


            builder.Services.AddSingleton(new FirebaseAuthClient(new FirebaseAuthConfig()
            {
                ApiKey = "AIzaSyCyLKylikL5dUKQEKxMn6EkY6PnBWKmJtA",
                AuthDomain = "stega-426008.firebaseapp.com",
                Providers = new FirebaseAuthProvider[]
                {
                    new EmailProvider()
                }
            }));

            return builder.Build();
        }
    }
}