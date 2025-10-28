using SliceX.ViewModels;
using System.Windows;

namespace SliceX.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Set the DataContext if it's not already set in XAML
            if (DataContext == null)
            {
                DataContext = new MainViewModel();
            }
        }

        // Handle window closing event
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.StatusMessage = "Closing application...";
            }
            base.OnClosing(e);
        }
    }
}