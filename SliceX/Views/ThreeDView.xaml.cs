using SliceX.ViewModels;
using System.Windows.Controls;
using HelixToolkit.Wpf;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Windows.Input;

namespace SliceX.Views
{
    public partial class ThreeDView : UserControl
    {
        public HelixViewport3D Viewport3D => this.viewport;
        
        private Point3D _cameraPosition;
        private Vector3D _cameraLookDirection;
        private Vector3D _cameraUpDirection;
        private double _cameraFieldOfView;
        private bool _isViewportInitialized = false;

        public ThreeDView()
        {
            InitializeComponent();
            this.Loaded += ThreeDView_Loaded;
            this.Unloaded += ThreeDView_Unloaded;
            
            // Enable drag-drop on the entire user control
            this.AllowDrop = true;
            
            // Hook up drag-drop events
            this.DragEnter += OnDragEnter;
            this.DragOver += OnDragOver;
            this.DragLeave += OnDragLeave;
            this.Drop += OnDrop;
            
            // Also enable drag-drop on the viewport
            if (viewport != null)
            {
                viewport.AllowDrop = true;
                viewport.DragEnter += OnDragEnter;
                viewport.DragOver += OnDragOver;
                viewport.DragLeave += OnDragLeave;
                viewport.Drop += OnDrop;
            }
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.HandleDragEnterCommand.Execute(e);
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            // This is crucial for drag-drop to work
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnDragLeave(object sender, DragEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.HandleDragLeaveCommand.Execute(null);
            }
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.HandleDropCommand.Execute(e);
            e.Handled = true;
            // Reset drag state
                viewModel.HandleDragLeaveCommand.Execute(null);
            }
        }

        private void ThreeDView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel viewModel)
                {
                    // Always ensure the viewport is connected to ViewModel
                    viewModel.Viewport3D = this.Viewport3D;
                    
                    // Restore camera state if we have saved values
                    if (_isViewportInitialized && Viewport3D.Camera != null)
                    {
                        RestoreCameraState();
                    }
                    else if (!_isViewportInitialized)
                    {
                        // First time initialization - ensure basic viewport elements exist
                        EnsureViewportElements();
                        _isViewportInitialized = true;
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ThreeDView_Loaded: {ex.Message}");
            }
        }

        private void ThreeDView_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Save camera state before unloading
                if (Viewport3D?.Camera is PerspectiveCamera camera)
                {
                    _cameraPosition = camera.Position;
                    _cameraLookDirection = camera.LookDirection;
                    _cameraUpDirection = camera.UpDirection;
                    _cameraFieldOfView = camera.FieldOfView;
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ThreeDView_Unloaded: {ex.Message}");
            }
        }

        private void EnsureViewportElements()
        {
            // Check if basic elements already exist
            bool hasLights = false;
            bool hasGrid = false;
            
            foreach (var child in Viewport3D.Children)
            {
                if (child is Light || (child is ModelVisual3D mv && mv.Content is Light))
                    hasLights = true;
                if (child is GridLinesVisual3D)
                    hasGrid = true;
            }
            
            // Add missing basic elements
            if (!hasLights)
                Viewport3D.Children.Add(new DefaultLights());
                
            if (!hasGrid)
            {
                var grid = new GridLinesVisual3D
                {
                    Width = 200,
                    Length = 200,
                    MinorDistance = 10,
                    MajorDistance = 50,
                    Thickness = 0.5,
                    Fill = new SolidColorBrush(Color.FromRgb(45, 45, 48))
                };
                Viewport3D.Children.Add(grid);
            }
        }

        private void RestoreCameraState()
        {
            if (Viewport3D?.Camera is PerspectiveCamera camera)
            {
                camera.Position = _cameraPosition;
                camera.LookDirection = _cameraLookDirection;
                camera.UpDirection = _cameraUpDirection;
                camera.FieldOfView = _cameraFieldOfView;
            }
        }

        public void OnActivated()
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.StatusMessage = "3D View activated";
                
                // Ensure viewport is connected to ViewModel
                if (viewModel.Viewport3D != this.Viewport3D)
                {
                    viewModel.Viewport3D = this.Viewport3D;
                }
            }
        }
    }
}