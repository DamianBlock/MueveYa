using Microsoft.Maui.Controls;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.ObjectModel;
using Microsoft.Maui.Graphics;
using System;
using System.Threading.Tasks;

namespace AppFletesMueve.Views
{
    public class ChatMessage
    {
        public int? Id { get; set; }
        public string User { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsMe { get; set; }
    }

    public class ChatPage : ContentPage
    {
        private readonly HubConnection _hub;
        private readonly string _groupName;
        private readonly string _userName;
        private readonly ObservableCollection<ChatMessage> _messages = new();
        private ListView _listView;

        private int _pageSize = 50;
        private int _loaded = 0;
        private int _total = 0;
        private bool _loading = false;

        private Entry _entry = new Entry { Placeholder = "Escribir mensaje..." };
        private Button _btn = new Button { Text = "Enviar" };

        public ChatPage(HubConnection hub, string groupName, string userName)
        {
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));
            _groupName = groupName ?? throw new ArgumentNullException(nameof(groupName));
            _userName = userName ?? throw new ArgumentNullException(nameof(userName));

            Title = "Chat";

            _listView = new ListView
            {
                ItemsSource = _messages,
                SeparatorVisibility = SeparatorVisibility.None,
                ItemTemplate = new DataTemplate(() =>
                {
                    var msgLabel = new Label { FontSize = 14 };
                    msgLabel.SetBinding(Label.TextProperty, "Message");
                    var ts = new Label { FontSize = 10, TextColor = Colors.Gray };
                    ts.SetBinding(Label.TextProperty, new Binding("Timestamp", stringFormat: "{0:HH:mm}"));

                    var frame = new Frame { Padding = 8, HasShadow = false, CornerRadius = 8 };
                    frame.Content = new StackLayout { Children = { msgLabel, ts } };

                    frame.BindingContextChanged += (s, e) =>
                    {
                        if (frame.BindingContext is ChatMessage cm)
                        {
                            frame.BackgroundColor = cm.IsMe ? Color.FromArgb("#DCF8C6") : Colors.LightGray;
                            frame.HorizontalOptions = cm.IsMe ? LayoutOptions.End : LayoutOptions.Start;
                        }
                    };

                    return new ViewCell { View = frame };
                })
            };

            _listView.ItemAppearing += async (s, e) =>
            {
                if (_loading) return;
                if (_messages.Count == 0) return;
                if (e.Item == _messages[0] && _loaded < _total)
                {
                    _loading = true;
                    if (_groupName.StartsWith("solicitud-") && int.TryParse(_groupName.Substring("solicitud-".Length), out var sid))
                    {
                        await LoadPageAsync(sid, _loaded, _pageSize);
                        _loaded = _messages.Count;
                    }
                    _loading = false;
                }
            };

            _btn.Clicked += async (s, e) => await SendMessageAsync();

            var panel = new Grid { RowDefinitions = { new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }, new RowDefinition { Height = GridLength.Auto } } };
            Grid.SetRow(_listView, 0);
            panel.Children.Add(_listView);

            var sendPanel = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } } };
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
                    var cm = new ChatMessage { User = user, Message = message, Timestamp = timestamp.ToLocalTime(), IsMe = user == _userName };
                    _messages.Add(cm);
                    _listView.ScrollTo(cm, ScrollToPosition.End, true);
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

                if (_groupName.StartsWith("solicitud-") && int.TryParse(_groupName.Substring("solicitud-".Length), out var sid))
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
                var http = new System.Net.Http.HttpClient();
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
                var cm = new ChatMessage { User = _userName, Message = text, Timestamp = DateTime.Now, IsMe = true };
                _messages.Add(cm);
                _listView.ScrollTo(cm, ScrollToPosition.End, true);
            }
            catch { }
        }

        private async Task LoadPageAsync(int solicitudId, int skip, int take)
        {
            try
            {
                var http = new System.Net.Http.HttpClient();
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
                    var list = new System.Collections.Generic.List<ChatMessage>();
                    foreach (var el in itemsEl.EnumerateArray())
                    {
                        var sender = el.GetProperty("sender").GetString() ?? string.Empty;
                        var msg = el.GetProperty("message").GetString() ?? string.Empty;
                        var ts = el.GetProperty("timestampUtc").GetDateTime();
                        list.Add(new ChatMessage { User = sender, Message = msg, Timestamp = ts.ToLocalTime(), IsMe = sender == _userName });
                    }
                    // prepend
                    list.Reverse();
                    foreach (var m in list) _messages.Insert(0, m);
                }
            }
            catch { }
        }
    }
}
