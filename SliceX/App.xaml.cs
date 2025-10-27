using System.Windows;
using System.Windows.Threading;

namespace SliceX
{
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"An unhandled exception occurred: {e.Exception.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Enable better error handling
            this.DispatcherUnhandledException += (s, args) => 
            {
                MessageBox.Show(args.Exception.ToString(), "Dispatcher Unhandled Exception");
                args.Handled = true;
            };
            
            AppDomain.CurrentDomain.UnhandledException += (s, args) => 
            {
                MessageBox.Show((args.ExceptionObject as Exception)?.ToString(), "Current Domain Unhandled Exception");
            };
        }
    }
}