using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;

namespace UniversalConnectionTester
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient HttpClient = new();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var configuration = await LoadConfigurationAsync();
                BuildButtons(configuration.Endpoints);
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Configuration error", $"Failed to load endpoints.json:\n{FormatException(ex)}");
            }
        }

        private async Task<EndpointConfiguration> LoadConfigurationAsync()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "endpoints.json");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Configuration file not found", path);
            }

            var json = await File.ReadAllTextAsync(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

            var configuration = JsonSerializer.Deserialize<EndpointConfiguration>(json, options);
            return configuration ?? new EndpointConfiguration();
        }

        private void BuildButtons(System.Collections.Generic.IEnumerable<EndpointDefinition> endpoints)
        {
            EndpointButtonsPanel.Children.Clear();

            if (!endpoints.Any())
            {
                EndpointButtonsPanel.Children.Add(new TextBlock
                {
                    Text = "No endpoints configured.",
                    Margin = new Thickness(0, 0, 0, 8)
                });
                return;
            }

            foreach (var endpoint in endpoints)
            {
                var button = new Button
                {
                    Content = endpoint.Name,
                    Tag = endpoint,
                    Margin = new Thickness(0, 0, 0, 8),
                    Padding = new Thickness(12, 8, 12, 8),
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };

                button.Click += async (_, _) => await HandleTestAsync(button, endpoint);
                EndpointButtonsPanel.Children.Add(button);
            }
        }

        private async Task HandleTestAsync(Button button, EndpointDefinition endpoint)
        {
            var originalBrush = button.Background;
            button.IsEnabled = false;
            button.Background = Brushes.LightGray;

            try
            {
                var result = await TestEndpointAsync(endpoint);
                button.Background = result.Success ? Brushes.LightGreen : Brushes.IndianRed;

                if (!result.Success)
                {
                    ShowErrorDialog($"{endpoint.Name} failed", result.ErrorMessage ?? "Unknown error");
                }
            }
            catch (Exception ex)
            {
                button.Background = Brushes.IndianRed;
                ShowErrorDialog($"{endpoint.Name} failed", FormatException(ex));
            }
            finally
            {
                button.IsEnabled = true;
                if (button.Background == Brushes.LightGray)
                {
                    button.Background = originalBrush;
                }
            }
        }

        private async Task<ConnectionTestResult> TestEndpointAsync(EndpointDefinition endpoint)
        {
            return endpoint.ConnectionType switch
            {
                ConnectionType.Mssql => await TestSqlServerAsync(endpoint.ConnectionString),
                ConnectionType.Oracle => await TestOracleAsync(endpoint.ConnectionString),
                ConnectionType.Http or ConnectionType.Https => await TestHttpAsync(endpoint.ConnectionString),
                ConnectionType.Ping => await TestPingAsync(endpoint.ConnectionString),
                _ => ConnectionTestResult.Fail("Unsupported connection type.")
            };
        }

        private static async Task<ConnectionTestResult> TestSqlServerAsync(string connectionString)
        {
            try
            {
                var cs = EnsureSqlTimeout(connectionString);
                await using var connection = new SqlConnection(cs);
                await connection.OpenAsync();
                return ConnectionTestResult.Ok();
            }
            catch (Exception ex)
            {
                return ConnectionTestResult.Fail(FormatException(ex));
            }
        }

        private static async Task<ConnectionTestResult> TestOracleAsync(string connectionString)
        {
            try
            {
                var cs = EnsureOracleTimeout(connectionString);
                await using var connection = new OracleConnection(cs);
                await connection.OpenAsync();
                return ConnectionTestResult.Ok();
            }
            catch (Exception ex)
            {
                return ConnectionTestResult.Fail(FormatException(ex));
            }
        }

        private static async Task<ConnectionTestResult> TestHttpAsync(string url)
        {
            try
            {
                using var response = await HttpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return ConnectionTestResult.Ok();
                }

                var body = await response.Content.ReadAsStringAsync();
                var snippet = string.IsNullOrWhiteSpace(body)
                    ? "<empty>"
                    : body.Length > 500 ? body[..500] + "..." : body;
                var error = $"HTTP {(int)response.StatusCode} - {response.ReasonPhrase}\nBody:\n{snippet}";
                return ConnectionTestResult.Fail(error);
            }
            catch (Exception ex)
            {
                return ConnectionTestResult.Fail(FormatException(ex));
            }
        }

        private static async Task<ConnectionTestResult> TestPingAsync(string host)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, 3000);
                if (reply.Status == IPStatus.Success)
                {
                    return ConnectionTestResult.Ok();
                }

                return ConnectionTestResult.Fail($"Ping failed: {reply.Status}");
            }
            catch (Exception ex)
            {
                return ConnectionTestResult.Fail(FormatException(ex));
            }
        }

        private static string FormatException(Exception ex)
        {
            if (ex.InnerException == null)
            {
                return $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
            }

            return $"{ex.GetType().Name}: {ex.Message}\nInner: {FormatException(ex.InnerException)}\n{ex.StackTrace}";
        }

        private static string EnsureSqlTimeout(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                ConnectTimeout = 10
            };
            return builder.ConnectionString;
        }

        private static string EnsureOracleTimeout(string connectionString)
        {
            var builder = new OracleConnectionStringBuilder(connectionString);
            builder["Connection Timeout"] = 10;
            return builder.ConnectionString;
        }

        private void ShowErrorDialog(string title, string message)
        {
            var dialog = new Window
            {
                Title = title,
                Owner = this,
                Width = 520,
                Height = 360,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.CanResize
            };

            var grid = new Grid
            {
                Margin = new Thickness(12)
            };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var textBox = new TextBox
            {
                Text = message ?? string.Empty,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                AcceptsTab = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new FontFamily("Consolas"),
                Background = Brushes.WhiteSmoke,
                BorderThickness = new Thickness(1)
            };

            var closeButton = new Button
            {
                Content = "Close",
                Width = 80,
                Margin = new Thickness(0, 12, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            closeButton.Click += (_, _) => dialog.Close();

            Grid.SetRow(textBox, 0);
            Grid.SetRow(closeButton, 1);
            grid.Children.Add(textBox);
            grid.Children.Add(closeButton);

            dialog.Content = grid;
            dialog.ShowDialog();
        }
    }

    public record ConnectionTestResult(bool Success, string? ErrorMessage = null)
    {
        public static ConnectionTestResult Ok() => new(true, null);
        public static ConnectionTestResult Fail(string? message) => new(false, message);
    }
}
