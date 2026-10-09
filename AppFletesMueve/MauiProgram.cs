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
                .UseMauiMaps() // ✅ Importante para mapas
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
            builder.Services.AddSingleton<PlacesService>();
            // Directions service registered for DI (also keep static Instance for backward compatibility)
            builder.Services.AddSingleton<IDirectionsService>(DirectionsService.Instance);
            // Register pages for DI
            builder.Services.AddTransient<HomeCliente>();
            builder.Services.AddTransient<HomeConductor>();
            builder.Services.AddTransient<SeleccionarCargaPage>();
            builder.Services.AddTransient<CompletarPerfilConductorPage>();
            builder.Services.AddTransient<RegistroPage>();
            builder.Services.AddSingleton<IChatPageFactory, ChatPageFactory>();

            return builder.Build();
        }
    }
}
