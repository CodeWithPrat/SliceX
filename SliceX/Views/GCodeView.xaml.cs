using SliceX.ViewModels;
using System.Windows.Controls;

namespace SliceX.Views
{
    public partial class GCodeView : UserControl
    {
        public GCodeView()
        {
            InitializeComponent();
            this.Loaded += GCodeView_Loaded;
        }

        private void GCodeView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Ensure the G-Code text box is scrolled to top and focused when view loads
            if (GCodeTextBox != null && !string.IsNullOrEmpty(GCodeTextBox.Text))
            {
                GCodeTextBox.ScrollToHome();
                GCodeTextBox.Focus();
            }
        }

        // Method to refresh the G-Code display
        public void RefreshGCode()
        {
            if (GCodeTextBox != null && DataContext is MainViewModel viewModel)
            {
                // Force refresh of the text binding
                GCodeTextBox.Text = viewModel.GeneratedGCode;
                
                if (!string.IsNullOrEmpty(GCodeTextBox.Text))
                {
                    GCodeTextBox.ScrollToHome();
                    GCodeTextBox.Focus();
                }
            }
        }

        // Method called when this view becomes active
        public void OnActivated()
        {
            RefreshGCode();
            
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.StatusMessage = "G-Code View activated";
            }
        }
    }
}