using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace WbTnvedManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            try
            {
                // Initialize SQLite PCL bundle explicitly
                SQLitePCL.Batteries_V2.Init();
            }
            catch (Exception ex)
            {
                LogAndShowError("Lỗi khởi tạo SQLite", ex);
            }

            base.OnStartup(e);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogAndShowError("Dispatcher Unhandled Exception", e.Exception);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogAndShowError("AppDomain Unhandled Exception", ex);
            }
            else
            {
                LogAndShowError("AppDomain Unhandled Exception (Unknown)", new Exception(e.ExceptionObject?.ToString() ?? "Unknown error"));
            }
        }

        private static void LogAndShowError(string context, Exception ex)
        {
            try
            {
                var logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt");
                var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}:\n{ex}\n\n";
                File.AppendAllText(logFile, message);
            }
            catch
            {
                // Ignore failure to write log
            }

            MessageBox.Show(
                $"Ứng dụng gặp lỗi:\n\n{ex.Message}\n\nChi tiết:\n{ex}",
                "Lỗi khởi chạy",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
