using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Easytorier
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string ServerUrl = "http://localhost:5000/api/copilot";

        // Active placement tool state
        private string _activeTool = "Select";

        public MainWindow()
        {
            InitializeComponent();
            LevelViewport.MouseLeftButtonDown += LevelViewport_MouseLeftButtonDown;
        }

        // --- Tool Selection Handlers ---
        private void OnSelectToolClick(object sender, RoutedEventArgs e) => _activeTool = "Select";
        private void OnWallToolClick(object sender, RoutedEventArgs e) => _activeTool = "Wall";
        private void OnObstacleToolClick(object sender, RoutedEventArgs e) => _activeTool = "Obstacle";
        private void OnTriggerToolClick(object sender, RoutedEventArgs e) => _activeTool = "Trigger";

        // --- Canvas Placer Logic ---
        private void LevelViewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_activeTool == "Select") return;

            Point clickPos = e.GetPosition(LevelViewport);

            UIElement spawnedElement = _activeTool switch
            {
                "Wall" => CreatePlatformElement(clickPos, Brushes.Gray, 120, 20),
                "Obstacle" => CreatePlatformElement(clickPos, Brushes.IndianRed, 40, 40),
                "Trigger" => CreatePlatformElement(clickPos, Brushes.Gold, 30, 30),
                _ => null
            };

            if (spawnedElement != null)
            {
                LevelViewport.Children.Add(spawnedElement);
            }
        }

        private Shape CreatePlatformElement(Point pos, Brush color, double width, double height)
        {
            var rect = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = color,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            Canvas.SetLeft(rect, pos.X - (width / 2));
            Canvas.SetTop(rect, pos.Y - (height / 2));
            return rect;
        }

        // --- AI Chat Logic ---
        private async void OnSendAiPrompt(object sender, RoutedEventArgs e)
        {
            string prompt = AiInputBox.Text.Trim();
            if (string.IsNullOrEmpty(prompt)) return;

            AiChatLog.Items.Add(new ListBoxItem
            {
                Content = $"You: {prompt}",
                Foreground = Brushes.White
            });

            AiInputBox.Clear();
            AiInputBox.IsEnabled = false;

            var loadingItem = new ListBoxItem
            {
                Content = "AI: Thinking...",
                Foreground = (Brush)FindResource("TextMuted")
            };
            AiChatLog.Items.Add(loadingItem);
            ScrollToBottom();

            string serverResponse = await SendToServerAsync(prompt);

            AiChatLog.Items.Remove(loadingItem);
            AiChatLog.Items.Add(new ListBoxItem
            {
                Content = $"AI: {serverResponse}",
                Foreground = (Brush)FindResource("AccentBlue")
            });

            AiInputBox.IsEnabled = true;
            AiInputBox.Focus();
            ScrollToBottom();
        }

        private async Task<string> SendToServerAsync(string userPrompt)
        {
            try
            {
                var payload = new { prompt = userPrompt };
                string jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.PostAsync(ServerUrl, content);
                if (!response.IsSuccessStatusCode)
                {
                    return $"Server Error: {response.ReasonPhrase}";
                }

                string responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);

                if (doc.RootElement.TryGetProperty("response", out var textProp))
                {
                    return textProp.GetString()?.Trim() ?? "Received empty response from server.";
                }

                return responseString;
            }
            catch (Exception ex)
            {
                return $"Error connecting to server: {ex.Message}";
            }
        }

        // --- Theme Manager Logic ---
        private void OnOpenThemeManager(object sender, RoutedEventArgs e)
        {
            var themeWindow = new Window
            {
                Title = "Theme Manager",
                Width = 350,
                Height = 320,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = (Brush)FindResource("BgDark"),
                ResizeMode = ResizeMode.NoResize
            };

            var stack = new StackPanel { Margin = new Thickness(15) };

            TextBox CreateColorRow(string labelText, string currentKey)
            {
                var lbl = new TextBlock { Text = labelText, Foreground = (Brush)FindResource("TextPrimary"), Margin = new Thickness(0, 0, 0, 2) };
                var txt = new TextBox
                {
                    Text = ((SolidColorBrush)FindResource(currentKey)).Color.ToString(),
                    Background = (Brush)FindResource("BgHeader"),
                    Foreground = (Brush)FindResource("TextPrimary"),
                    BorderBrush = (Brush)FindResource("BorderDark"),
                    Padding = new Thickness(4),
                    Margin = new Thickness(0, 0, 0, 10)
                };
                stack.Children.Add(lbl);
                stack.Children.Add(txt);
                return txt;
            }

            var bgInput = CreateColorRow("Background Color (Hex e.g. #1E1E1E)", "BgDark");
            var panelInput = CreateColorRow("Panel Color (Hex)", "BgPanel");
            var accentInput = CreateColorRow("Accent Color (Hex)", "AccentBlue");

            var applyButton = new Button
            {
                Content = "Apply Theme",
                Style = (Style)FindResource("PrimaryButton"),
                HorizontalAlignment = HorizontalAlignment.Right,
                Padding = new Thickness(10, 6)
            };

            applyButton.Click += (s, ev) =>
            {
                try
                {
                    if (ColorConverter.ConvertFromString(bgInput.Text) is Color bgCol)
                        Application.Current.Resources["BgDark"] = new SolidColorBrush(bgCol);

                    if (ColorConverter.ConvertFromString(panelInput.Text) is Color panelCol)
                        Application.Current.Resources["BgPanel"] = new SolidColorBrush(panelCol);

                    if (ColorConverter.ConvertFromString(accentInput.Text) is Color accentCol)
                        Application.Current.Resources["AccentBlue"] = new SolidColorBrush(accentCol);

                    themeWindow.Close();
                }
                catch
                {
                    MessageBox.Show("Invalid Hex color format. Please use formats like #1E1E1E.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            stack.Children.Add(applyButton);
            themeWindow.Content = stack;
            themeWindow.ShowDialog();
        }

        private void ScrollToBottom()
        {
            if (AiChatLog.Items.Count > 0)
            {
                AiChatLog.ScrollIntoView(AiChatLog.Items[AiChatLog.Items.Count - 1]);
            }
        }
    }
}