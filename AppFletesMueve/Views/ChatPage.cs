using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;

namespace AppFletesMueve.Views
{
    public class ChatMessage
    {
        public int? Id { get; set; }
        public string User { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsMe { get; set; }

        // Para bindear el estilo de la burbuja sin código en el template
        public LayoutOptions Alineacion => IsMe ? LayoutOptions.End : LayoutOptions.Start;
        public Color ColorFondo => IsMe ? Color.FromArgb("#DCF8C6") : Colors.LightGray;
    }

    public class ChatPage : ContentPage
    {
        private readonly HubConnection _hub;
        private readonly string _groupName;
        private readonly string _userName;
        private readonly ObservableCollection<ChatMessage> _messages = new();
        private readonly CollectionView _lista;

        private readonly int _pageSize = 50;
        private int _loaded;
        private int _total;
        private bool _loading;

        private readonly Entry _entry = new() { Placeholder = "Escribir mensaje..." };
        private readonly Button _btn = new() { Text = "Enviar" };

        public ChatPage(HubConnection hub, string groupName, string userName)
        {
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));
            _groupName = groupName ?? throw new ArgumentNullException(nameof(groupName));
            _userName = userName ?? throw new ArgumentNullException(nameof(userName));

            Title = "Chat";

            _lista = new CollectionView
            {
                ItemsSource = _messages,
                SelectionMode = SelectionMode.None,
                ItemsUpdatingScrollMode = ItemsUpdatingScrollMode.KeepItemsInView,
                ItemTemplate = new DataTemplate(() =>
                {
                    var msgLabel = new Label { FontSize = 14, TextColor = Colors.Black };
                    msgLabel.SetBinding(Label.TextProperty, nameof(ChatMessage.Message));

                    var ts = new Label { FontSize = 10, TextColor = Colors.Gray };
                    ts.SetBinding(Label.TextProperty, new Binding(nameof(ChatMessage.Timestamp), stringFormat: "{0:HH:mm}"));

                    var burbuja = new Border
                    {
                        Padding = 8,
                        Margin = new Thickness(8, 2),
                        StrokeThickness = 0,
                        MaximumWidthRequest = 280,
                        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) },
                        Content = new VerticalStackLayout { Children = { msgLabel, ts } }
                    };
                    burbuja.SetBinding(VisualElement.BackgroundColorProperty, nameof(ChatMessage.ColorFondo));
                    burbuja.SetBinding(View.HorizontalOptionsProperty, nameof(ChatMessage.Alineacion));

                    return burbuja;
                })
            };

            // Cargar mensajes más viejos cuando el usuario llega al principio de la lista
            _lista.Scrolled += async (s, e) =>
            {
                if (_loading || _messages.Count == 0) return;
                if (e.FirstVisibleItemIndex > 0) return;
                if (_loaded >= _total) return;

                if (_groupName.StartsWith("solicitud-") &&
                    int.TryParse(_groupName.Substring("solicitud-".Length), out var sid))
                {
                    _loading = true;
                    try
                    {
                        await LoadPageAsync(sid, _loaded, _pageSize);
                        _loaded = _messages.Count;
                    }
                    finally
                    {
                        _loading = false;
                    }
                }
            };

            _btn.Clicked += async (s, e) => await SendMessageAsync();

            var panel = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };
            Grid.SetRow(_lista, 0);
            panel.Children.Add(_lista);

            var sendPanel = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };
            sendPanel.Children.Add(_entry);
            Grid.SetColumn(_btn, 1);
            sendPanel.Children.Add(_btn);

            Grid.SetRow(sendPanel, 1);
            panel.Children.Add(sendPanel);

            Content = panel;

            _hub.On<string, string, DateTime>("ReceiveMessage", (user, message, timestamp) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var cm = new ChatMessage
                    {
                        User = user,
                        Message = message,
                        Timestamp = timestamp.ToLocalTime(),
                        IsMe = user == _userName
                    };
                    _messages.Add(cm);
                    _lista.ScrollTo(cm, position: ScrollToPosition.End, animate: true);
                });
            });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                if (_hub.State != HubConnectionState.Connected) await _hub.StartAsync();
                await _hub.SendAsync("JoinGroup", _groupName);

                if (_groupName.StartsWith("solicitud-") &&
                    int.TryParse(_groupName.Substring("solicitud-".Length), out var sid))
                {
                    await LoadPageAsync(sid, 0, _pageSize);
                    _loaded = _messages.Count;

                    // Marcar como leídos los mensajes visibles al abrir (hasta ahora)
                    try
                    {
                        await MarkReadBySolicitudAsync(sid, DateTime.UtcNow, _userName);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private async Task MarkReadBySolicitudAsync(int solicitudId, DateTime beforeUtc, string readBy)
        {
            try
            {
                var http = new HttpClient();
#if DEBUG
                var apiBase = "http://10.0.2.2:5051/api/";
#else
                var apiBase = "https://mueveya.onrender.com/api/";
#endif
                var url = apiBase + $"Chat/mark-read-by-solicitud/{solicitudId}?before={System.Net.WebUtility.UrlEncode(beforeUtc.ToString("o"))}&readBy={System.Net.WebUtility.UrlEncode(readBy)}";
                await http.PostAsync(url, null);
            }
            catch { }
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            try
            {
                // Marcar como leídos los mensajes que llegaron mientras el chat estaba abierto
                if (_groupName.StartsWith("solicitud-") &&
                    int.TryParse(_groupName.Substring("solicitud-".Length), out var sid))
                {
                    await MarkReadBySolicitudAsync(sid, DateTime.UtcNow, _userName);
                }

                if (_hub.State == HubConnectionState.Connected) await _hub.SendAsync("LeaveGroup", _groupName);
                _hub.Remove("ReceiveMessage");
            }
            catch { }
        }

        private async Task SendMessageAsync()
        {
            var text = _entry.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return;
            try
            {
                if (_hub.State != HubConnectionState.Connected) await _hub.StartAsync();
                await _hub.SendAsync("SendMessageToGroup", _groupName, _userName, text);
                _entry.Text = string.Empty;
                // El mensaje se agrega cuando vuelve del servidor (ReceiveMessage), así no se duplica
            }
            catch { }
        }

        private async Task LoadPageAsync(int solicitudId, int skip, int take)
        {
            try
            {
                var http = new HttpClient();
#if DEBUG
                var apiBase = "http://10.0.2.2:5051/api/";
#else
                var apiBase = "https://mueveya.onrender.com/api/";
#endif
                var resp = await http.GetAsync(apiBase + $"Chat/solicitud/{solicitudId}?skip={skip}&take={take}");
                if (!resp.IsSuccessStatusCode) return;

                var json = await resp.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("total", out var totalEl)) _total = totalEl.GetInt32();
                if (root.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var list = new List<ChatMessage>();
                    foreach (var el in itemsEl.EnumerateArray())
                    {
                        var sender = el.GetProperty("sender").GetString() ?? string.Empty;
                        var msg = el.GetProperty("message").GetString() ?? string.Empty;
                        var ts = el.GetProperty("timestampUtc").GetDateTime();
                        list.Add(new ChatMessage { User = sender, Message = msg, Timestamp = ts.ToLocalTime(), IsMe = sender == _userName });
                    }

                    // Los más viejos van arriba
                    list.Reverse();
                    foreach (var m in list) _messages.Insert(0, m);
                }
            }
            catch { }
        }
    }
}