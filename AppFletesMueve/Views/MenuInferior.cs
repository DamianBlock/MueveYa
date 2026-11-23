using AndroidPlatform = Microsoft.Maui.Controls.PlatformConfiguration.Android;
using AndroidTabbed = Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.TabbedPage;
using Placement = Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.ToolbarPlacement;

namespace AppFletesMueve.Views;

// Base común: barra de pestañas ABAJO y sin deslizar entre pestañas
// (para que los gestos del mapa no cambien de pantalla sin querer).
public abstract class MenuInferiorPage : TabbedPage
{
    protected MenuInferiorPage()
    {
        BarBackgroundColor = Colors.White;
        SelectedTabColor = Color.FromArgb("#E65100");
        UnselectedTabColor = Color.FromArgb("#757575");

        AndroidTabbed.SetToolbarPlacement(On<AndroidPlatform>(), Placement.Bottom);
        AndroidTabbed.SetIsSwipePagingEnabled(On<AndroidPlatform>(), false);
    }

    // Cada pestaña tiene su propia navegación: así las pantallas internas
    // (chat, mis vehículos, viaje) se abren dentro de la pestaña.
    protected void Agregar(Page pagina, string titulo, string icono)
    {
        Children.Add(new NavigationPage(pagina)
        {
            Title = titulo,
            IconImageSource = icono
        });
    }
}

public class MenuConductorPage : MenuInferiorPage
{
    public MenuConductorPage(HomeConductor inicio)
    {
        Agregar(inicio, "Inicio", "ic_inicio.png");
        Agregar(new ViajesPage(esConductor: true), "Viajes", "ic_viajes.png");
        Agregar(new SeccionEnConstruccionPage("Billetera",
            "Acá vas a ver tus ganancias, los cobros de cada viaje y vas a poder retirar tu saldo."), "Billetera", "ic_billetera.png");
        Agregar(new SeccionEnConstruccionPage("Promos",
            "Acá vas a ver incentivos y bonos para conductores."), "Promos", "ic_promos.png");
        Agregar(new AyudaPage(), "Ayuda", "ic_ayuda.png");
    }
}

public class MenuClientePage : MenuInferiorPage
{
    public MenuClientePage(HomeCliente inicio)
    {
        Agregar(inicio, "Inicio", "ic_inicio.png");
        Agregar(new ViajesPage(esConductor: false), "Viajes", "ic_viajes.png");
        Agregar(new SeccionEnConstruccionPage("Promos",
            "Acá vas a ver descuentos y promociones."), "Promos", "ic_promos.png");
        Agregar(new AyudaPage(), "Ayuda", "ic_ayuda.png");
    }
}
