using Microsoft.Maui.Controls.Shapes;

namespace AppFletesMueve.Views;

public sealed class LegalPage : ContentPage
{
    public LegalPage(bool privacidad)
    {
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F5F5F5");
        var titulo = privacidad ? "Política de privacidad" : "Términos de uso";
        var texto = privacidad ? TextoPrivacidad : TextoTerminos;
        var raiz = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) }
        };
        raiz.Add(SeccionesUi.Cabecera(titulo), 0, 0);
        raiz.Add(new ScrollView { Content = new VerticalStackLayout { Padding = 18, Spacing = 14, Children = { Tarjeta(texto), new Label { Text = "Documento operativo pendiente de revisión legal antes del lanzamiento público.", FontSize = 12, TextColor = Color.FromArgb("#757575") } } } }, 0, 1);
        Content = raiz;
    }

    private static Border Tarjeta(string texto) => new()
    {
        BackgroundColor = Colors.White,
        Stroke = Color.FromArgb("#E0E0E0"),
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
        Padding = 16,
        Content = new Label { Text = texto, TextColor = Color.FromArgb("#303030"), FontSize = 14, LineHeight = 1.35 }
    };

    private const string TextoPrivacidad = "MUEVE recopila datos de registro (nombre, correo, teléfono y DNI), ubicación cuando utilizás el servicio, datos del viaje y mensajes de chat. Usamos estos datos para crear la cuenta, conectar clientes y conductores, calcular y realizar viajes, prevenir fraude y brindar soporte.\n\nLa ubicación y datos necesarios del viaje se comparten únicamente entre las personas participantes. Para mapas y búsqueda de direcciones usamos servicios de Google. No vendemos datos personales.\n\nConservamos los datos mientras la cuenta esté activa y, cuando corresponda, por los plazos necesarios para reclamos, seguridad u obligaciones legales. Podés solicitar la eliminación desde Ayuda. Consultas: gestion24hs@gmail.com.";
    private const string TextoTerminos = "MUEVE facilita la coordinación entre clientes y conductores. Cada persona debe brindar información verdadera, utilizar la app de forma segura y respetuosa y cumplir las normas aplicables.\n\nLas tarifas estimadas, promociones y disponibilidad se informan antes de solicitar un viaje. Las promociones están sujetas a sus condiciones, vigencia y topes. La billetera actual es informativa: MUEVE todavía no procesa cobros, retiros ni transferencias desde la aplicación.\n\nLos viajes pueden cancelarse desde la app; las cancelaciones quedan registradas. Para soporte o reclamos escribinos a gestion24hs@gmail.com.";
}
