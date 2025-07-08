using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Text.Json;
using SphereSSL_TrayIcon.Model;
using Avalonia.Threading;
using Avalonia.Controls.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia;

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
        internal static Avalonia.Controls.TrayIcon trayIcon;
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
                    if (OperatingSystem.IsWindows())
                    {
                        // Avalonia folder picker using StorageProvider API
                        var tcs = new TaskCompletionSource<string?>();

                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            var topLevel = TopLevel.GetTopLevel(Application.Current.ApplicationLifetime switch
                            {
                                IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
                                _ => null
                            });

                            if (topLevel?.StorageProvider != null)
                            {
                                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                                {
                                    AllowMultiple = false,
                                    Title = "Select Folder"
                                });

                                selectedPath = folders?.FirstOrDefault()?.Path?.ToString() ?? "";
                            }

                            tcs.SetResult(selectedPath);
                        });

                        selectedPath = await tcs.Task ?? "";
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        try
                        {
                            var process = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = "zenity",
                                    Arguments = "--file-selection --directory",
                                    RedirectStandardOutput = true,
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                }
                            };

                            process.Start();
                            selectedPath = process.StandardOutput.ReadLine() ?? "";
                            process.WaitForExit();
                        }
                        catch
                        {
                            selectedPath = "";
                        }
                    }

                    var buffer = Encoding.UTF8.GetBytes(selectedPath);
                    context.Response.ContentLength64 = buffer.Length;
                    await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    context.Response.OutputStream.Close();
                }
                catch
                {
                    var errorBuffer = Encoding.UTF8.GetBytes("Error selecting folder.");
                    context.Response.ContentLength64 = errorBuffer.Length;
                    await context.Response.OutputStream.WriteAsync(errorBuffer, 0, errorBuffer.Length);
                    context.Response.OutputStream.Close();
                }
                finally
                {
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
                        decodedPath = decodedPath.Replace("file:///", "").Replace('/', Path.DirectorySeparatorChar);

                    if (Directory.Exists(decodedPath))
                    {
                        if (OperatingSystem.IsWindows())
                        {
                            Process.Start("explorer.exe", $"\"{decodedPath}\"");
                        }
                        else if (OperatingSystem.IsLinux())
                        {
                            try
                            {
                                Process.Start("xdg-open", decodedPath);
                            }
                            catch
                            {
                                // handle if xdg-open fails
                            }
                        }
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
                        if (Console.IsOutputRedirected || Console.OpenStandardOutput(1) != Stream.Null)
                            Console.WriteLine("SphereSSLv2.exe not found!");
                        return;
                    }
                }
                catch { }

                return;
            }

            if (!File.Exists(kestrelPath))
            {
               
                Console.WriteLine("SphereSSLv2.exe not found!");
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
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = $"-Command \"[reflection.assembly]::loadwithpartialname('System.Windows.Forms');" +
                                    $"[System.Windows.Forms.MessageBox]::Show('Folder opened: {decodedPath.Replace("\\", "\\\\")}')\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }
                else if (OperatingSystem.IsLinux())
                {
                    Process.Start("notify-send", $"\"Folder opened\" \"{decodedPath}\"");
                }
                else
                {
                    Console.WriteLine($"Folder opened: {decodedPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Notification failed: " + ex.Message);
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
