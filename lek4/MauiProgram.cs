﻿using Firebase.Auth;
using Firebase.Auth.Providers;
using lek4.Components.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plugin.MauiMTAdmob;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http;
using System;

namespace lek4
{
    public static class MauiProgram
    {
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

            // Registrera andra tjänster
            builder.Services.AddSingleton<GoogleFitService>();
            builder.Services.AddSingleton<AdService>();
            builder.Services.AddSingleton<UserService>();
            builder.Services.AddSingleton<Components.Service.NumberService>();

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
