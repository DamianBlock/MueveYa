using Microsoft.Maui.Controls.Shapes;

namespace AppFletesMueve.Views;

// Pantallas simples para las pestañas del menú inferior.
// Se construyen por código (sin XAML) para mantener todo en un solo archivo.

internal static class SeccionesUi
{
    public static Grid Cabecera(string titulo)
    {
        return new Grid
        {
            BackgroundColor = Color.FromArgb("#E65100"),
            Padding = new Thickness(15, 14),
            Children =
            {
                new Label
                {
                    Text = titulo,
                    TextColor = Colors.White,
                    FontSize = 20,
                    FontAttributes = FontAttributes.Bold,
                    VerticalOptions = LayoutOptions.Center
                }
            }
        };
    }
}

// Pestañas que todavía no tienen contenido (Viajes, Billetera, Promos)
public class SeccionEnConstruccionPage : ContentPage
{
    public SeccionEnConstruccionPage(string titulo, string descripcion)
    {
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F5F5F5");

        var contenido = new VerticalStackLayout
        {
            Spacing = 10,
            Padding = new Thickness(30),
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = "Próximamente",
                    FontSize = 22,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#E65100"),
                    HorizontalTextAlignment = TextAlignment.Center
                },
                new Label
                {
                    Text = descripcion,
                    FontSize = 15,
                    TextColor = Color.FromArgb("#757575"),
                    HorizontalTextAlignment = TextAlignment.Center
                }
            }
        };

        var raiz = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };

        raiz.Add(SeccionesUi.Cabecera(titulo), 0, 0);
        raiz.Add(contenido, 0, 1);

        Content = raiz;
    }
}

// Pestaña de ayuda y soporte (ícono de auriculares)
public class AyudaPage : ContentPage
{
    // COMPLETAR con los datos reales de soporte
    // WhatsApp: código de país + código de área + número, sin "+" ni espacios (ej: 5493811234567)
    private const string WhatsAppSoporte = "5493816171541";
    private const string CorreoSoporte = "gestion24hs@gmail.com";

    public AyudaPage()
    {
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Color.FromArgb("#F5F5F5");

        var btnWhatsApp = Boton("Escribir por WhatsApp", Color.FromArgb("#2E7D32"), Colors.White, null);
        btnWhatsApp.Clicked += async (_, _) =>
            await AbrirAsync($"https://wa.me/{WhatsAppSoporte}?text={Uri.EscapeDataString("Hola, necesito ayuda con MUEVE")}");

        var btnCorreo = Boton("Enviar un correo", Colors.White, Color.FromArgb("#E65100"), Color.FromArgb("#E65100"));
        btnCorreo.Clicked += async (_, _) =>
            await AbrirAsync($"mailto:{CorreoSoporte}?subject={Uri.EscapeDataString("Ayuda con MUEVE")}");
        var btnPrivacidad = Boton("Política de privacidad", Colors.White, Color.FromArgb("#E65100"), Color.FromArgb("#E65100"));
        btnPrivacidad.Clicked += async (_, _) => await Navigation.PushAsync(new LegalPage(privacidad: true));
        var btnTerminos = Boton("Términos de uso", Colors.White, Color.FromArgb("#E65100"), Color.FromArgb("#E65100"));
        btnTerminos.Clicked += async (_, _) => await Navigation.PushAsync(new LegalPage(privacidad: false));
        var btnEliminar = Boton("Solicitar eliminación de cuenta", Colors.White, Color.FromArgb("#C62828"), Color.FromArgb("#C62828"));
        btnEliminar.Clicked += SolicitarEliminacionAsync;

        var contenido = new VerticalStackLayout
        {
            Spacing = 14,
            Padding = new Thickness(16),
            Children =
            {
                new Label
                {
                    Text = "¿Necesitás ayuda?",
                    FontSize = 20,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#212121")
                },
                new Label
                {
                    Text = "Escribinos y te respondemos lo antes posible.",
                    FontSize = 14,
                    TextColor = Color.FromArgb("#757575")
                },
                btnWhatsApp,
                btnCorreo,
                btnPrivacidad,
                btnTerminos,
                btnEliminar,
                new Label
                {
                    Text = "Preguntas frecuentes",
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#212121"),
                    Margin = new Thickness(0, 10, 0, 0)
                },
                Pregunta("¿Cómo se calcula la tarifa?",
                    "Depende de la distancia, el peso de la carga y el tipo de vehículo. Incluye 15 minutos de espera para cargar y 15 para descargar."),
                Pregunta("¿Puedo cancelar un viaje?",
                    "Sí, desde la pantalla del viaje, eligiendo un motivo. Las cancelaciones quedan registradas."),
                Pregunta("¿Cómo hablo con la otra persona?",
                    "Desde la pantalla del viaje, con el botón de chat.")
            }
        };

        var raiz = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };

        raiz.Add(SeccionesUi.Cabecera("Ayuda"), 0, 0);
        raiz.Add(new ScrollView { Content = contenido }, 0, 1);

        Content = raiz;
    }

    private static Button Boton(string texto, Color fondo, Color colorTexto, Color? borde)
    {
        var boton = new Button
        {
            Text = texto,
            BackgroundColor = fondo,
            TextColor = colorTexto,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 50,
            CornerRadius = 12
        };

        if (borde != null)
        {
            boton.BorderColor = borde;
            boton.BorderWidth = 1;
        }

        return boton;
    }

    private static Border Pregunta(string pregunta, string respuesta)
    {
        return new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
            StrokeThickness = 0,
            BackgroundColor = Colors.White,
            Padding = 14,
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label
                    {
                        Text = pregunta,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#212121")
                    },
                    new Label
                    {
                        Text = respuesta,
                        FontSize = 13,
                        TextColor = Color.FromArgb("#757575")
                    }
                }
            }
        };
    }

    private async Task AbrirAsync(string url)
    {
        try
        {
            await Launcher.Default.OpenAsync(new Uri(url));
        }
        catch
        {
            await DisplayAlertAsync("Ayuda", "No se pudo abrir la aplicación. Probá escribirnos por otro medio.", "Aceptar");
        }
    }

    private async void SolicitarEliminacionAsync(object? sender, EventArgs e)
    {
        var confirmar = await DisplayAlertAsync("Eliminar cuenta", "La solicitud será revisada por soporte. Los datos pueden conservarse temporalmente por reclamos u obligaciones legales. ¿Querés continuar?", "Solicitar", "Cancelar");
        if (!confirmar) return;

        var resultado = await new AppFletesMueve.Services.UsuarioService().SolicitarEliminacionAsync();
        await DisplayAlertAsync("MUEVE", resultado.Mensaje, "Aceptar");
    }
}
