using SliceX.ViewModels;
using System.Windows.Controls;
using HelixToolkit.Wpf;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Media;

namespace SliceX.Views
{
    public partial class ThreeDView : UserControl
    {
        public HelixViewport3D Viewport3D => this.Viewport3DControl;
        
        // Properties to preserve viewport state
        private Point3D _cameraPosition;
        private Vector3D _cameraLookDirection;
        private Vector3D _cameraUpDirection;
        private double _cameraFieldOfView;
        private bool _isViewportInitialized = false;
        private ModelVisual3D _cachedModelVisual;

        public ThreeDView()
        {
            InitializeComponent();
            this.Loaded += ThreeDView_Loaded;
            this.Unloaded += ThreeDView_Unloaded;
        }

        private void ThreeDView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel viewModel)
                {
                    // Always ensure the viewport is connected to ViewModel
                    viewModel.Viewport3D = this.Viewport3D;
                    
                    // If we have a cached model, restore it
                    if (_cachedModelVisual != null && !Viewport3D.Children.Contains(_cachedModelVisual))
                    {
                        Viewport3D.Children.Add(_cachedModelVisual);
                    }
                    
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
                
                // Cache the current model visual if it exists
                if (DataContext is MainViewModel viewModel && viewModel.CurrentModel != null)
                {
                    // Find the model visual in the viewport
                    foreach (var child in Viewport3D.Children)
                    {
                        if (child is ModelVisual3D modelVisual && modelVisual.Content is GeometryModel3D)
                        {
                            _cachedModelVisual = modelVisual;
                            break;
                        }
                    }
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

        // Method to clear cached model (when loading a new model)
        public void ClearCachedModel()
        {
            _cachedModelVisual = null;
        }
    }
}