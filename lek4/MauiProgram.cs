using Firebase.Auth;
using Firebase.Auth.Providers;
using lek4.Components.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plugin.MauiMTAdmob;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http;
using System;
using Blazored.LocalStorage;
using Plugin.Maui.Audio;

namespace lek4
{
    public static class MauiProgram
    {
        public static FirebaseAuthProvider FirebaseClient { get; private set; }
        public static FirebaseAuthConfig FirebaseConfig { get; private set; }

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiMTAdmob()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();

            // Registrera HttpClient med basadress till ditt API
            builder.Services.AddHttpClient("ApiClient", client =>
            {
                client.BaseAddress = new Uri("http://10.66.14.184:5146/"); // Kontrollera att detta är korrekt
            });
            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ApiClient"));

            // Registrera HttpClient som standardtjänst för andra API-anrop
            builder.Services.AddHttpClient();

            builder.Services.AddScoped<CustomAuthenticationStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CustomAuthenticationStateProvider>());
            builder.Services.AddAuthorizationCore();
            // Registrera Authorization som en scoped tjänst
            builder.Services.AddScoped<Authorization>();

            builder.Services.AddBlazoredLocalStorage();

            // Registrera andra tjänster
            builder.Services.AddSingleton<GoogleFitService>();
            builder.Services.AddSingleton<AutoDrawService>();
            builder.Services.AddSingleton<AdService>();
            builder.Services.AddSingleton<UserService>();
            builder.Services.AddSingleton<ProductDrawDateService>();
            builder.Services.AddSingleton<ProductService>();
            builder.Services.AddScoped<DrawService>();
            builder.Services.AddScoped<DrawJackpotService>();
            builder.Services.AddScoped<JackpotService>();
            builder.Services.AddScoped<StatsService>();
            builder.Services.AddSingleton<Components.Service.NumberService>();
            builder.Services.AddScoped<DailyRewardService>(); 
            builder.Services.AddScoped<InfoConfigService>();
            builder.Services.AddScoped<CharityService>();
            builder.Services.AddScoped<BoostFriendService>();
            builder.Services.AddScoped<CommunityService>();
            builder.Services.AddScoped<SpecialService>();
            builder.Services.AddScoped<WinnerCleanupService>();
            builder.Services.AddScoped<StorageService>();

            builder.Services.AddSingleton(AudioManager.Current);
            builder.Services.AddTransient<MainPage>();

            // Konfigurera Firebase Authentication med din API-nyckel
            builder.Services.AddFirebaseAuth("AIzaSyCyLKylikL5dUKQEKxMn6EkY6PnBWKmJtA");

            // Skapa och registrera FirebaseAuthClient för hantering av Firebase-användare
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
