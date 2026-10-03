using AppFletesMueve.Services;
using AppFletesMueve.Views;
using Microsoft.Extensions.Logging;

namespace AppFletesMueve
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiMaps()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton<UsuarioService>();
            builder.Services.AddSingleton<TransporteService>();
            // Registrar MAUI Maps
            builder.Services.AddMauiMaps();
            builder.Services.AddTransient<RegistroPage>();

            return builder.Build();
        }
    }
}
