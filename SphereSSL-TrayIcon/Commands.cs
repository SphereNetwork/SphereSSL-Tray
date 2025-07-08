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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.Text.Json;
using SphereSSL_TrayIcon.Model;


namespace SphereSSL_TrayIcon
{
    public class Commands
    {
        internal static Process kestrelProcess;
        internal static string kestrelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SphereSSLv2.exe");
        internal static string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SphereSSL.ico");
        internal static bool FolderOpen = false;
        internal static int ServerPort = 7171; // Default port for SphereSSL
        internal static int ListenerPort = 7172; // Port for the HTTP listener
        internal static string ListenerUrl = $"127.0.0.1"; // Base URL for SphereSSL
        internal static string ServerUrl = $"127.0.0.1"; // Base URL for SphereSSL
        internal static NotifyIcon trayIcon;
        internal static string ConfigFilePath = "app.config";
        internal static string DefaultConfigFilePath = "default.config";
        internal static string dbPath = "cachedtempdata.dll"; // Default database path

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static async Task StartAppListener()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://{ListenerUrl}:{ListenerPort}/select-folder/");
            listener.Prefixes.Add($"http://{ListenerUrl}:{ListenerPort}/open-location/");
            listener.Prefixes.Add($"http://{ListenerUrl}:{ListenerPort}/restart/");
            listener.Prefixes.Add($"http://{ListenerUrl}:{ListenerPort}/factory-reset/");
            listener.Prefixes.Add($"http://{ListenerUrl}:{ListenerPort}/update-db-path/");
            listener.Prefixes.Add($"http://{ListenerUrl}:{ListenerPort}/update-url-path/");
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
                   
                    await RestartServer(context);
                    break;

                case "/factory-reset/":
                    Console.WriteLine("factory-reset-heard");
                    await FactoryReset(context);
                    break;

                case "/update-db-path/":
                 
                    await ChangeDbPath(context);
                    break;

                case "/update-url-path/":
                    
                    await ChangeServerPath(context);
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
                                Opacity = 0,
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

            if (kestrelProcess != null && !kestrelProcess.HasExited)
            {
                kestrelProcess.Kill();
            }
        }

        public static async Task RestartServer(HttpListenerContext context)
        {
            var response = context.Response;
            try
            {
                Console.WriteLine("Killing server...");
                await KillServer();
                Console.WriteLine("Restarting server...");
                await StartServer();

                Console.WriteLine("Server restarted.");

                response.StatusCode = 200;
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("OK"));
                response.Close();
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error: {ex.Message}");
                response.StatusCode = 409;
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Error Restarting Server."));
                response.Close();
            }
        }

        public static async Task RestartServerTray()
        {

            try
            {
                Console.WriteLine("Killing server...");
                await KillServer();
                Console.WriteLine("Restarting server...");
                await StartServer();

                Console.WriteLine("Server restarted.");

            }
            catch (Exception ex)
            {


            }
        }

        public static async Task FactoryReset(HttpListenerContext context)
        {

                    Console.WriteLine("Performing factory reset...");
            var request = context.Request;

            var response = context.Response;

            try
            {
                Console.WriteLine("Killing server...");
                await KillServer();

                if (System.IO.File.Exists(DefaultConfigFilePath))
                {
                        System.IO.File.Copy(DefaultConfigFilePath, ConfigFilePath, true);
                }

                Console.WriteLine("Resetting Config file..");

                await StartServer();

                Console.WriteLine("Server restarted Reset Complete.");
                response.StatusCode = 200;
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("OK"));
                    response.Close();
       
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                response.StatusCode = 400;
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Error performing factory reset."));
                response.Close();

            }
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

        internal static async Task<StoredConfig> LoadConfigFile()
        {
            try
            {

                string json = File.ReadAllText(ConfigFilePath);

                var storedConfig = JsonSerializer.Deserialize<StoredConfig>(json);
                Console.WriteLine(storedConfig.ToString());
                if (storedConfig == null)
                {
                    throw new InvalidOperationException("Failed to deserialize node config.");
                }
              

                ServerPort = storedConfig.ServerPort > 0 ? storedConfig.ServerPort : 7171;

                ServerUrl = string.IsNullOrWhiteSpace(storedConfig.ServerURL)
                ? "127.0.0.1"
                : storedConfig.ServerURL;


                dbPath = string.IsNullOrWhiteSpace(storedConfig.DatabasePath)
                    ? "cachedtempdata.dll"
                    : storedConfig.DatabasePath;

                return storedConfig;

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to load config file.", ex);
            }
        }

        public static async Task ChangeServerPath(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            Console.WriteLine("ChangeServerPath called");
            try
            {
                await KillServer();
                string body;
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    body = await reader.ReadToEndAsync();
                }

              
                var parsedRequest = JsonSerializer.Deserialize<UpdateServerRequest>(body);


                StoredConfig config = await LoadConfigFile();
                ServerUrl = parsedRequest.ServerUrl;
                ServerPort = parsedRequest.ServerPort;

                config.ServerURL = parsedRequest.ServerUrl;
                config.ServerPort = parsedRequest.ServerPort;

                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(ConfigFilePath, json);

                await Task.Delay(200);

                await StartServer();

                response.StatusCode = 200;
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("OK"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                response.StatusCode = 400;
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Error changing database path."));
            }
            finally
            {
                response.Close();
            }
        }

        public static async Task ChangeDbPath(HttpListenerContext context)
        {
            Console.WriteLine("ChangeDbPath called");
            var request = context.Request;
            var rawPath = request.QueryString["path"];
            var response = context.Response;

            try
            {
                await KillServer();
              
                if (!string.IsNullOrWhiteSpace(rawPath))
                {
                    StoredConfig config =await LoadConfigFile();

                    var oldPath = config.DatabasePath;
                    dbPath = rawPath;
                   
                    config.DatabasePath = dbPath;

                    
                    string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(ConfigFilePath, json);
                    await Task.Delay(200);

                    await StartServer();

                    response.StatusCode = 200;
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("OK"));
                    response.Close();
                }
                else
                {
                    response.StatusCode = 400;
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Invalid path for factory reset."));
                    response.Close();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                response.StatusCode = 400;
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Error changing database path."));
                response.Close();
            }
        }
    }
}
