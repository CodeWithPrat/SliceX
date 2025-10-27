using SliceX.ViewModels;
using System.Windows;

namespace SliceX.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.Viewport3D = viewport;
            }
        }
    }
}