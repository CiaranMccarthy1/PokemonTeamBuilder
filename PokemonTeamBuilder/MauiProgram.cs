using Microsoft.Extensions.Logging;

namespace PokemonTeamBuilder
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
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            builder.Services.AddSingleton<HttpClient>();

            builder.Services.AddSingleton<PokemonService>();       
            builder.Services.AddSingleton<PokemonBulkDownloader>();  

            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<LoadingPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
