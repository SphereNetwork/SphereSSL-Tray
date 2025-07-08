using SphereSSL_TrayIcon.Model;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace SphereSSL_TrayIcon
{
    internal static class Program
    {


        [STAThread]
        public static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += async (s, e) =>
            {
                await Commands.KillServer();
            };

            Commands.trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "SphereSSL"

            };

            if (File.Exists(Commands.iconPath))
            {
                Commands.trayIcon.Icon = new Icon(Commands.iconPath);
            }
            else
            {
                Commands.trayIcon.Icon = SystemIcons.Application;
            }


            if (File.Exists(Commands.ConfigFilePath))
            {
                var storedConfig = new StoredConfig();
                for (int i = 0; i < 3; i++)
                {
                    string json = File.ReadAllText(Commands.ConfigFilePath);

                    storedConfig = JsonSerializer.Deserialize<StoredConfig>(json);
                    Thread.Sleep(100);

                    if (!string.IsNullOrWhiteSpace(json) && json.Trim() != "{}")
                        break;
                }

                if (storedConfig == null)
                {
                    throw new InvalidOperationException("Failed to deserialize node config.");
                }

                Commands.ServerPort = storedConfig.ServerPort;
                Commands.ServerUrl = storedConfig.ServerURL;
                Commands.dbPath = storedConfig.DatabasePath;

            }
            else
            {

                Commands.ServerUrl = "127.0.0.1";
                Commands.ServerPort = 7171;
                Commands.dbPath = "cachedtempdata.dll";


            }



            Commands.trayIcon.ContextMenuStrip = new ContextMenuStrip();

            Commands.trayIcon.ContextMenuStrip.Items.Add("Open SphereSSL", null, (s, e) =>
            {
                Console.WriteLine($"Opening SphereSSL UI at {Commands.ServerUrl}:{Commands.ServerPort}...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"http://{Commands.ServerUrl}:{Commands.ServerPort}",
                    UseShellExecute = true
                });
            });

            Commands.trayIcon.ContextMenuStrip.Items.Add("Restart SphereSSL", null, async (s, e) =>
            {

                await Commands.RestartServerTray();
            });

            Commands.trayIcon.ContextMenuStrip.Items.Add("Help", null, (s, e) =>
            {

                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/kl3mta3/SphereSSLv2",
                    UseShellExecute = true
                });

            });

            Commands.trayIcon.ContextMenuStrip.Items.Add("Exit", null, async (s, e) =>
            {
                Commands.trayIcon.Visible = false;
                await Commands.KillServer();
                Environment.Exit(0);
            });

            Commands.trayIcon.DoubleClick += (s, e) =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"http://{Commands.ServerUrl}:{Commands.ServerPort}",
                    UseShellExecute = true
                });
            };

            await Commands.StartServer();

            Commands.trayIcon.BalloonTipTitle = "SphereSSL Ready";
            Commands.trayIcon.BalloonTipText = "Double Click to open UI or right-click for options.";
            Commands.trayIcon.ShowBalloonTip(3000);

            _ = Task.Run(Commands.StartAppListener);

            Application.Run();
        }
    }
}
    
