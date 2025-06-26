using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;


namespace SphereSSL_TrayIcon
{
    public class Commands
    {
        internal static Process kestrelProcess;
        internal static string kestrelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SphereSSLv2.exe");
        internal static string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SphereSSL.ico");
        internal static bool FolderOpen = false;

        internal static NotifyIcon trayIcon;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static async Task StartAppListener()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:7172/select-folder/");
            listener.Prefixes.Add("http://localhost:7172/open-location/");
            listener.Start();




            while (true)
            {
     
                await ProcessRequest(listener).ConfigureAwait(false);
            }
        }

        public static async Task ProcessRequest(HttpListener listener)
        {


            var context = await listener.GetContextAsync();
            var response = context.Response;
       

            if (context == null || context.Request.Url == null)
            {

                response.StatusCode = 400; 
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Invalid request"));
                response.Close();
                return;
            }

            switch (context.Request.Url.AbsolutePath)
            {
                case "/select-folder/":
                 
                    await GetFolderPath(context);
                    break;

                case "/open-location/":
                 
                    await OpenPathLocation(context);
                    break;

                case "/restart/":

                    await RestartServer();
                    break;


                default:

                    response.StatusCode = 404; 
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Invalid request"));
                    response.Close();
                    return;
            }
        }

        public static async Task GetFolderPath(HttpListenerContext context)
        {
         
            string selectedPath = "";
            if (!FolderOpen)
            {
                FolderOpen = true;

                try
                {
                    var t = new Thread(() =>
                    {
                        using var form = new Form
                        {
                            StartPosition = FormStartPosition.CenterScreen,
                            TopMost = true,
                            ShowInTaskbar = false,
                            WindowState = FormWindowState.Normal,
                            FormBorderStyle = FormBorderStyle.FixedToolWindow, // Actually shows up
                            Opacity = 0.01,
                            Size = new System.Drawing.Size(300, 200)
                        };

                        form.Shown += (s, e) =>
                        {
                            form.TopMost = true;
                            form.BringToFront();
                            form.Activate();
                            using var dialog = new FolderBrowserDialog();
                            if (dialog.ShowDialog(form) == DialogResult.OK)
                            {
                                selectedPath = dialog.SelectedPath;
                            }
                            form.Close();
                        };

                        Application.Run(form);
                    });

                    t.SetApartmentState(ApartmentState.STA);
                    t.Start();
                    t.Join();

                    var buffer = Encoding.UTF8.GetBytes(selectedPath);
                    context.Response.ContentLength64 = buffer.Length;
                    await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    context.Response.OutputStream.Close();
                    FolderOpen = false;
                }
                catch (Exception ex)
                {
                   
                    var errorBuffer = Encoding.UTF8.GetBytes("Error selecting folder.");
                    context.Response.ContentLength64 = errorBuffer.Length;
                    await context.Response.OutputStream.WriteAsync(errorBuffer, 0, errorBuffer.Length);
                    context.Response.OutputStream.Close();
                    FolderOpen = false;
                }
               
            }
        }
        public static async Task OpenPathLocation(HttpListenerContext context)
        {
            var request = context.Request;
            var rawPath = request.QueryString["path"];
            var response = context.Response;

            try
            {
                if (!string.IsNullOrWhiteSpace(rawPath))
                {
                    var decodedPath = WebUtility.UrlDecode(rawPath);
                    if (decodedPath.StartsWith("file:///"))
                        decodedPath = decodedPath.Replace("file:///", "").Replace('/', '\\');

                    if (Directory.Exists(decodedPath))
                    {
                        var t = new Thread(() =>
                        {
                            using var form = new Form
                            {
                                StartPosition = FormStartPosition.CenterScreen,
                                TopMost = true,
                                ShowInTaskbar = false,
                                WindowState = FormWindowState.Normal,
                                FormBorderStyle = FormBorderStyle.None,
                                Opacity = 0,         // Fully invisible!
                                Size = new System.Drawing.Size(1, 1)
                            };

                            form.Shown += (s, e) =>
                            {
                                form.TopMost = true;
                                form.BringToFront();
                                form.Activate();

                                Process.Start("explorer.exe", $"\"{decodedPath}\"");

                                Task.Delay(300).ContinueWith(_ => form.Close());
                            };

                            Application.Run(form);
                        });
                        t.SetApartmentState(ApartmentState.STA);
                        t.Start();
                        t.Join();

                        // Set the Tag so the click handler knows which path to use
                        trayIcon.Tag = decodedPath;

                        // Remove previous handlers (if any) before adding new one
                        trayIcon.BalloonTipClicked -= TrayIcon_BalloonTipClicked;
                        trayIcon.BalloonTipClicked += TrayIcon_BalloonTipClicked;

                        trayIcon.BalloonTipTitle = "Folder Opened";
                        trayIcon.BalloonTipText = "If you don't see the folder, click here to open it again.";
                        trayIcon.ShowBalloonTip(3000);
                    }
                }
                response.StatusCode = 200;
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("OK"));
                response.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                response.StatusCode = 400;
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Error opening path."));
                response.Close();
            }
        }

        // Handler for retrying on click
        private static void TrayIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            if (sender is NotifyIcon ni && ni.Tag is string decodedPath && Directory.Exists(decodedPath))
            {
                var t = new Thread(() =>
                {
                    using var form = new Form
                    {
                        StartPosition = FormStartPosition.CenterScreen,
                        TopMost = true,
                        ShowInTaskbar = true,
                        Size = new System.Drawing.Size(200, 100)
                    };
                    form.Shown += (ss, ee) =>
                    {
                        form.BringToFront();
                        form.Activate();
                        Process.Start("explorer.exe", $"\"{decodedPath}\"");
                        Task.Delay(300).ContinueWith(_ => form.Close());
                    };
                    Application.Run(form);
                });
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
                t.Join();
            }
        }

        public static async Task StartServer()
        {
            var processName = Path.GetFileNameWithoutExtension(kestrelPath); 


            var existing = Process.GetProcessesByName(processName).FirstOrDefault();
            if (existing != null && !existing.HasExited)
            {
               
                try
                {
                    
                    if (existing.MainModule.FileName != kestrelPath)
                    {
                        kestrelProcess = existing;
                        Console.WriteLine($"Using existing process: {existing.Id} ({existing.MainModule.FileName})");
                        return;
                    }
                }
                catch
                {  

                }

                return;
            }

            if (!File.Exists(kestrelPath))
            {
                MessageBox.Show("SphereSSLv2.exe not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            kestrelProcess = new Process();
            kestrelProcess.StartInfo.FileName = kestrelPath;
            kestrelProcess.StartInfo.UseShellExecute = true;
            kestrelProcess.Start();
        }

        public static async Task KillServer()
        {
            var processName = Path.GetFileNameWithoutExtension(kestrelPath);
            var existing = Process.GetProcessesByName(processName).FirstOrDefault();

            Console.WriteLine($"Killing Process: {existing.Id} ({existing.MainModule.FileName})");

            if (kestrelProcess != null && !kestrelProcess.HasExited)
            {
                kestrelProcess.Kill();
            }
        }

        public static async Task RestartServer()
        {
            await KillServer();
            await Task.Delay(2000);
            await StartServer();
        }



        public static void ShowFolderOpenedNotification(string decodedPath)
        {
            trayIcon.BalloonTipTitle = "Folder Opened";
            trayIcon.BalloonTipText = "If you don't see the folder, click here to open it again.";

            trayIcon.BalloonTipClicked -= NotifyIcon_BalloonTipClicked; // Avoid double-registration
            trayIcon.BalloonTipClicked += NotifyIcon_BalloonTipClicked;

            trayIcon.Tag = decodedPath; // Store path for event handler

            trayIcon.ShowBalloonTip(3000);
        }

        private static void NotifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            var ni = sender as NotifyIcon;
            if (ni?.Tag is string decodedPath && Directory.Exists(decodedPath))
            {
                // This time, pop up a visible window for guaranteed focus (user-initiated)
                var t = new Thread(() =>
                {
                    using var form = new Form
                    {
                        StartPosition = FormStartPosition.CenterScreen,
                        TopMost = true,
                        ShowInTaskbar = true, // Visible this time!
                        Size = new System.Drawing.Size(200, 100)
                    };
                    form.Shown += (ss, ee) =>
                    {
                        form.BringToFront();
                        form.Activate();
                        Process.Start("explorer.exe", $"\"{decodedPath}\"");
                        Task.Delay(300).ContinueWith(_ => form.Close());
                    };
                    Application.Run(form);
                });
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
                t.Join();
            }
        }

    }
}
