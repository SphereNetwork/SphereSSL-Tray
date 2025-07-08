using Avalonia.Controls;
using Avalonia;
using SphereSSL_TrayIcon.Model;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Platform;
using Avalonia.Win32;
using Avalonia.Threading;



namespace SphereSSL_TrayIcon
{
    public class App : Application
    {
        public override void OnFrameworkInitializationCompleted()
        {
            base.OnFrameworkInitializationCompleted();

            // Tray setup AFTER Avalonia has initialized.
            Dispatcher.UIThread.Post(async () =>
            {
                await Program.SetupTrayAsync();
            });
        }

    }

    internal class Program
    {
        public static TrayIcon? trayIcon;

        // Entry Point
        public static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += async (s, e) =>
            {
                await Commands.KillServer();
            };

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

          
    
        }

        internal static async Task SetupTrayAsync()
        {
            try
            {
                // Load config early
                if (File.Exists(Commands.ConfigFilePath))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var json = File.ReadAllText(Commands.ConfigFilePath);
                        if (!string.IsNullOrWhiteSpace(json) && json.Trim() != "{}")
                        {
                            var storedConfig = JsonSerializer.Deserialize<StoredConfig>(json);
                            if (storedConfig != null)
                            {
                                Commands.ServerUrl = storedConfig.ServerURL;
                                Commands.ServerPort = storedConfig.ServerPort;
                                Commands.dbPath = storedConfig.DatabasePath;
                                break;
                            }
                        }
                        await Task.Delay(100);
                    }
                }
                else
                {
                    Commands.ServerUrl = "127.0.0.1";
                    Commands.ServerPort = 7171;
                    Commands.dbPath = "cachedtempdata.dll";
                }

                // Build menu first
                var menu = new NativeMenu();

                var openItem = new NativeMenuItem("Open SphereSSL");
                openItem.Click += (_, __) =>
                {
                    Console.WriteLine($"Opening SphereSSL UI at {Commands.ServerUrl}:{Commands.ServerPort}...");
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = $"http://{Commands.ServerUrl}:{Commands.ServerPort}",
                        UseShellExecute = true
                    });
                };
                menu.Items.Add(openItem);

                var restartItem = new NativeMenuItem("Restart SphereSSL");
                restartItem.Click += async (_, __) =>
                {
                    Console.WriteLine("Restarting SphereSSL...");
                    await Commands.RestartServerTray();
                };
                menu.Items.Add(restartItem);

                var helpItem = new NativeMenuItem("Help");
                helpItem.Click += (_, __) =>
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/kl3mta3/SphereSSLv2",
                        UseShellExecute = true
                    });
                };
                menu.Items.Add(helpItem);

                var exitItem = new NativeMenuItem("Exit");
                exitItem.Click += async (_, __) =>
                {
                    Console.WriteLine("Exiting SphereSSL...");
                    if (trayIcon != null)
                        trayIcon.IsVisible = false;

                    await Commands.KillServer();
                    Environment.Exit(0);
                };
                menu.Items.Add(exitItem);

                // Ensure icon path exists
                if (!File.Exists(Commands.iconPath))
                {
                    Console.WriteLine($"Icon file not found at {Commands.iconPath}");
                }

                // Init tray icon last
                trayIcon = new TrayIcon
                {
                    ToolTipText = "SphereSSL",
                    Icon = new WindowIcon(Commands.iconPath),
                    Menu = menu,
                    IsVisible = true
                };

                Console.WriteLine($"Tray icon initialized with {menu.Items.Count} menu items");

                // Start backend listener
                _ = Task.Run(Commands.StartAppListener);

                // Notify
                if (OperatingSystem.IsLinux())
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "notify-send",
                        Arguments = "\"SphereSSL Ready\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "osascript",
                        Arguments = "-e 'display notification'",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tray setup failed: {ex.Message}");
            }
        }

        // Avalonia bootstrap
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                         .UsePlatformDetect()
                         .LogToTrace()
                         .AfterSetup(_ => SetupTrayAsync());
    }

}


