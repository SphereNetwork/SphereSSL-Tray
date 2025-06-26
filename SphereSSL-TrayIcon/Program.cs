using System.Diagnostics;
using System.Net;
using System.Text;

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

            Commands.trayIcon.ContextMenuStrip = new ContextMenuStrip();

            Commands.trayIcon.ContextMenuStrip.Items.Add("Open SphereSSL", null, (s, e) =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "http://localhost:7171",
                    UseShellExecute = true
                });
            });

            Commands.trayIcon.ContextMenuStrip.Items.Add("Restart SphereSSL", null, async (s, e) =>
            {
                await Commands.RestartServer();
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
                    FileName = "http://localhost:7171",
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