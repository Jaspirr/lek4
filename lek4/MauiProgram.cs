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

            // Register NumberService
            builder.Services.AddSingleton<Components.Service.NumberService>();

            return builder.Build();
        }
    }
}
