using System;
using System.IO;
using System.Windows;
using WinNotch.Services;
using WinNotch.ViewModels;

namespace WinNotch
{
    public partial class App : Application
    {
        private MediaService? _mediaService;
        private SystemStatusService? _systemStatusService;
        private ClipboardService? _clipboardService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                LogException(args.ExceptionObject as Exception);
            };
            DispatcherUnhandledException += (s, args) =>
            {
                LogException(args.Exception);
                args.Handled = true;
            };

            try
            {
                _mediaService = new MediaService();
                _systemStatusService = new SystemStatusService();
                _clipboardService = new ClipboardService();
                var codingAgentService = new CodingAgentService();

                var agentVm = new AgentViewModel(codingAgentService);
                var mediaVm = new MediaViewModel(_mediaService);
                var todoVm = new TodoViewModel();
                var focusVm = new TimerViewModel();
                var clipsVm = new ClipboardViewModel();

                var mainVm = new MainViewModel(agentVm, mediaVm, todoVm, focusVm, clipsVm, _systemStatusService, codingAgentService);

                var mainWindow = new MainWindow(mainVm, _clipboardService, _systemStatusService, codingAgentService);
                MainWindow = mainWindow;
                mainWindow.Show();
                mainWindow.Activate();

                _ = _mediaService.InitializeAsync();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private static void LogException(Exception? ex)
        {
            if (ex == null) return;
            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinNotch");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "error.log"), $"[{DateTime.Now}] {ex}\n");
            }
            catch { }
        }
    }
}
