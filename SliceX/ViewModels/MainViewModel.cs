using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelixToolkit.Wpf;
using SliceX.Models;
using SliceX.Slicer;
using SliceX.Utilities;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Win32;
using System.Linq;

namespace SliceX.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private Models.Model3D? currentModel;

        [ObservableProperty]
        private PrinterSettings printerSettings = new PrinterSettings();

        [ObservableProperty]
        private string statusMessage = "Ready";

        [ObservableProperty]
        private bool isModelLoaded = false;

        [ObservableProperty]
        private ObservableCollection<string> recentFiles = new ObservableCollection<string>();

        [ObservableProperty]
        private ObservableCollection<string> availableProfiles = new ObservableCollection<string>();

        [ObservableProperty]
        private string selectedProfile = "default";

        [ObservableProperty]
        private int selectedTabIndex = 0;

        // Transformation properties
        [ObservableProperty]
        private double moveStep = 10.0;

        [ObservableProperty]
        private double rotateStep = 90.0;

        [ObservableProperty]
        private double scaleValue = 1.0;

        [ObservableProperty]
        private double scaleX = 1.0;

        [ObservableProperty]
        private double scaleY = 1.0;

        [ObservableProperty]
        private double scaleZ = 1.0;

        // Viewport control properties
        [ObservableProperty]
        private double rotationSensitivity = 1.0;

        [ObservableProperty]
        private double panSensitivity = 1.0;

        [ObservableProperty]
        private double zoomSensitivity = 1.0;

        private HelixViewport3D? viewport3D;

        public HelixViewport3D? Viewport3D
        {
            get => viewport3D;
            set
            {
                viewport3D = value;
                if (viewport3D != null)
                {
                    InitializeViewport();
                }
            }
        }

        private readonly SliceX.Utilities.ModelImporter modelImporter = new SliceX.Utilities.ModelImporter();
        private readonly SlicingEngine slicingEngine = new SlicingEngine();
        private ModelVisual3D? currentModelVisual;

        public MainViewModel()
        {
            LoadAvailableProfiles();
        }

        private void LoadAvailableProfiles()
        {
            AvailableProfiles.Clear();
            foreach (var profile in ProfileManager.GetAvailableProfiles())
            {
                AvailableProfiles.Add(profile);
            }
        }

        private void InitializeViewport()
        {
            if (Viewport3D == null) return;

            // Clear any existing mouse bindings
            Viewport3D.InputBindings.Clear();

            // Basic viewport settings
            Viewport3D.ShowCoordinateSystem = true;
            Viewport3D.ShowCameraInfo = false;
            Viewport3D.ZoomExtentsWhenLoaded = true;

            // Enable all interactions
            Viewport3D.IsRotationEnabled = true;
            Viewport3D.IsPanEnabled = true;
            Viewport3D.IsZoomEnabled = true;
            Viewport3D.IsInertiaEnabled = true;
            Viewport3D.IsMoveEnabled = true;
            Viewport3D.IsChangeFieldOfViewEnabled = false;

            // Camera settings for smooth operation
            Viewport3D.CameraMode = CameraMode.Inspect;
            Viewport3D.CameraRotationMode = CameraRotationMode.Trackball;

            // Configure sensitivity
            Viewport3D.RotationSensitivity = RotationSensitivity;
            Viewport3D.UpDownPanSensitivity = PanSensitivity;
            Viewport3D.LeftRightPanSensitivity = PanSensitivity;
            Viewport3D.ZoomSensitivity = ZoomSensitivity;

            // Mouse behavior settings
            Viewport3D.RotateAroundMouseDownPoint = true;
            Viewport3D.ZoomAroundMouseDownPoint = true;
            Viewport3D.InfiniteSpin = false;

            // Set custom mouse gestures for precise control
            ConfigureMouseGestures();

            // Cursor settings
            Viewport3D.PanCursor = Cursors.SizeAll;
            Viewport3D.RotateCursor = Cursors.Hand;
            Viewport3D.ZoomCursor = Cursors.SizeNS;

            // Add default lighting
            Viewport3D.Children.Add(new DefaultLights());

            // Add grid
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

        private void ConfigureMouseGestures()
        {
            if (Viewport3D == null) return;

            // Configure mouse gestures for optimal control
            Viewport3D.RotateGesture = new MouseGesture(MouseAction.LeftClick);
            Viewport3D.PanGesture = new MouseGesture(MouseAction.RightClick);
            Viewport3D.ZoomGesture = new MouseGesture(MouseAction.MiddleClick);
            Viewport3D.ZoomGesture2 = new MouseGesture(MouseAction.WheelClick);
            
            // Disable other gestures that might interfere
            Viewport3D.ChangeFieldOfViewGesture = null;
            Viewport3D.ZoomRectangleGesture = null;

            // Fine-tune camera controller settings
            if (Viewport3D.CameraController != null)
            {
                Viewport3D.CameraController.CameraRotationMode = CameraRotationMode.Trackball;
                Viewport3D.CameraController.InfiniteSpin = false;
                Viewport3D.CameraController.RotationSensitivity = 1.0;
                Viewport3D.CameraController.ZoomSensitivity = 1.0;
                Viewport3D.CameraController.UpDownPanSensitivity = 1.0;
                Viewport3D.CameraController.LeftRightPanSensitivity = 1.0;
                Viewport3D.CameraController.IsInertiaEnabled = true;
            }
        }

        [RelayCommand]
        private void LoadModel()
        {
            if (Viewport3D == null) return;

            var dialog = new OpenFileDialog
            {
                Filter = "3D Models (*.stl;*.obj)|*.stl;*.obj|STL Files (*.stl)|*.stl|OBJ Files (*.obj)|*.obj|All files (*.*)|*.*",
                Multiselect = false,
                Title = "Select 3D Model"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    StatusMessage = "Loading model...";
                    CurrentModel = modelImporter.ImportModel(dialog.FileName);

                    if (CurrentModel == null)
                    {
                        StatusMessage = "Failed to load model";
                        return;
                    }

                    // Clear previous model
                    Viewport3D.Children.Clear();
                    Viewport3D.Children.Add(new DefaultLights());

                    // Add coordinate system grid
                    var grid = new GridLinesVisual3D
                    {
                        Width = Math.Max((float)printerSettings.BuildVolumeX + 40, 200),
                        Length = Math.Max((float)printerSettings.BuildVolumeY + 40, 200),
                        MinorDistance = 10,
                        MajorDistance = 50,
                        Thickness = 0.5,
                        Center = new Point3D(0, 0, 0),
                        Fill = new SolidColorBrush(Color.FromRgb(45, 45, 48))
                    };
                    Viewport3D.Children.Add(grid);

                    // Add build plate
                    var buildPlate = new BoxVisual3D
                    {
                        Width = printerSettings.BuildVolumeX,
                        Length = printerSettings.BuildVolumeY,
                        Height = 2,
                        Center = new Point3D(0, 0, -1),
                        Fill = new SolidColorBrush(Color.FromArgb(100, 0, 116, 204))
                    };
                    Viewport3D.Children.Add(buildPlate);

                    // Add build volume wireframe
                    var boundingBox = new BoundingBoxWireFrameVisual3D
                    {
                        BoundingBox = new Rect3D(
                            -printerSettings.BuildVolumeX / 2,
                            -printerSettings.BuildVolumeY / 2,
                            0,
                            printerSettings.BuildVolumeX,
                            printerSettings.BuildVolumeY,
                            printerSettings.BuildVolumeZ
                        ),
                        Thickness = 2,
                        Color = Color.FromRgb(0, 212, 255)
                    };
                    Viewport3D.Children.Add(boundingBox);

                    // Create material for the model
                    var modelMaterial = MaterialHelper.CreateMaterial(Colors.SteelBlue);
                    var backMaterial = MaterialHelper.CreateMaterial(Colors.LightSteelBlue);

                    // Add new model to viewport
                    currentModelVisual = new ModelVisual3D
                    {
                        Content = new GeometryModel3D
                        {
                            Geometry = CurrentModel.Geometry,
                            Material = modelMaterial,
                            BackMaterial = backMaterial
                        },
                        Transform = CurrentModel.Transform
                    };

                    Viewport3D.Children.Add(currentModelVisual);

                    // Center the model on build plate
                    CenterModel();

                    Viewport3D.ZoomExtents(500);

                    IsModelLoaded = true;
                    StatusMessage = $"Model loaded: {Path.GetFileName(dialog.FileName)} ({CurrentModel.Triangles.Count} triangles)";

                    if (!RecentFiles.Contains(dialog.FileName))
                    {
                        RecentFiles.Insert(0, dialog.FileName);
                        if (RecentFiles.Count > 10)
                        {
                            RecentFiles.RemoveAt(RecentFiles.Count - 1);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading model: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusMessage = "Error loading model";
                    IsModelLoaded = false;
                }
            }
        }

        // FIXED: Helper method to get current bounding box considering all transformations
        private Rect3D GetTransformedBounds()
        {
            if (CurrentModel == null) return new Rect3D();

            var geometry = CurrentModel.Geometry;
            var transform = CurrentModel.Transform;

            if (geometry == null || geometry.Positions.Count == 0)
                return new Rect3D();

            // Get bounds of all transformed vertices
            var transformedPoints = geometry.Positions.Select(p => transform.Transform(p)).ToList();
            
            if (transformedPoints.Count == 0)
                return new Rect3D();

            double minX = transformedPoints.Min(p => p.X);
            double maxX = transformedPoints.Max(p => p.X);
            double minY = transformedPoints.Min(p => p.Y);
            double maxY = transformedPoints.Max(p => p.Y);
            double minZ = transformedPoints.Min(p => p.Z);
            double maxZ = transformedPoints.Max(p => p.Z);

            return new Rect3D(minX, minY, minZ, maxX - minX, maxY - minY, maxZ - minZ);
        }

        // FIXED: Center model while preserving rotation and scale
        [RelayCommand]
        private void CenterModel()
        {
            if (CurrentModel == null || Viewport3D == null) return;

            // Get current transformed bounds
            var bounds = GetTransformedBounds();
            
            // Calculate center of transformed model
            var centerX = bounds.X + bounds.SizeX / 2;
            var centerY = bounds.Y + bounds.SizeY / 2;
            var centerZ = bounds.Z + bounds.SizeZ / 2;

            // Create translation to move center to origin and lift to half height
            var centerTranslation = new TranslateTransform3D(-centerX, -centerY, -centerZ + bounds.SizeZ / 2);

            // Get existing transform group or create new one
            var transformGroup = CurrentModel.Transform as Transform3DGroup ?? new Transform3DGroup();
            
            // Add the centering translation
            transformGroup.Children.Add(centerTranslation);
            
            CurrentModel.Transform = transformGroup;
            UpdateModelVisual();
            
            StatusMessage = "Model centered (rotation preserved)";
        }

        // FIXED: Place on platform while preserving rotation and scale
        [RelayCommand]
        private void PlaceOnPlatform()
        {
            if (CurrentModel == null || Viewport3D == null) return;

            // Get current transformed bounds
            var bounds = GetTransformedBounds();
            
            // Calculate center and bottom of transformed model
            var centerX = bounds.X + bounds.SizeX / 2;
            var centerY = bounds.Y + bounds.SizeY / 2;
            var bottomZ = bounds.Z; // Bottom of the model

            // Create translation to center XY and place bottom at Z=0
            var platformTranslation = new TranslateTransform3D(-centerX, -centerY, -bottomZ);

            // Get existing transform group or create new one
            var transformGroup = CurrentModel.Transform as Transform3DGroup ?? new Transform3DGroup();
            
            // Add the platform placement translation
            transformGroup.Children.Add(platformTranslation);
            
            CurrentModel.Transform = transformGroup;
            UpdateModelVisual();
            
            StatusMessage = "Model placed on platform (rotation preserved)";
        }

        [RelayCommand]
        private void MoveXPlus() => MoveModel(MoveStep, 0, 0);
        [RelayCommand]
        private void MoveXMinus() => MoveModel(-MoveStep, 0, 0);
        [RelayCommand]
        private void MoveYPlus() => MoveModel(0, MoveStep, 0);
        [RelayCommand]
        private void MoveYMinus() => MoveModel(0, -MoveStep, 0);
        [RelayCommand]
        private void MoveZPlus() => MoveModel(0, 0, MoveStep);
        [RelayCommand]
        private void MoveZMinus() => MoveModel(0, 0, -MoveStep);

        private void MoveModel(double deltaX, double deltaY, double deltaZ)
        {
            if (CurrentModel == null || currentModelVisual == null) return;

            var currentTransform = CurrentModel.Transform as Transform3DGroup ?? new Transform3DGroup();
            var translateTransform = new TranslateTransform3D(deltaX, deltaY, deltaZ);

            currentTransform.Children.Add(translateTransform);
            CurrentModel.Transform = currentTransform;
            UpdateModelVisual();

            StatusMessage = $"Moved: X{deltaX:+0.##;-0.##} Y{deltaY:+0.##;-0.##} Z{deltaZ:+0.##;-0.##}";
        }

        // Rotate Commands
        [RelayCommand]
        private void RotateXPlus() => RotateModel(RotateStep, 0, 0);
        [RelayCommand]
        private void RotateXMinus() => RotateModel(-RotateStep, 0, 0);
        [RelayCommand]
        private void RotateYPlus() => RotateModel(0, RotateStep, 0);
        [RelayCommand]
        private void RotateYMinus() => RotateModel(0, -RotateStep, 0);
        [RelayCommand]
        private void RotateZPlus() => RotateModel(0, 0, RotateStep);
        [RelayCommand]
        private void RotateZMinus() => RotateModel(0, 0, -RotateStep);

        private void RotateModel(double deltaX, double deltaY, double deltaZ)
        {
            if (CurrentModel == null || currentModelVisual == null) return;

            var currentTransform = CurrentModel.Transform as Transform3DGroup ?? new Transform3DGroup();

            // Get current bounds to rotate around current center
            var bounds = GetTransformedBounds();
            var rotationCenter = new Point3D(
                bounds.X + bounds.SizeX / 2,
                bounds.Y + bounds.SizeY / 2,
                bounds.Z + bounds.SizeZ / 2
            );

            if (deltaX != 0)
                currentTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), deltaX), rotationCenter));
            if (deltaY != 0)
                currentTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), deltaY), rotationCenter));
            if (deltaZ != 0)
                currentTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), deltaZ), rotationCenter));

            CurrentModel.Transform = currentTransform;
            UpdateModelVisual();

            StatusMessage = $"Rotated: X{deltaX:+0;-0}° Y{deltaY:+0;-0}° Z{deltaZ:+0;-0}°";
        }

        // Scale Commands
        [RelayCommand]
        private void ScaleUniform()
        {
            ScaleModel(ScaleValue, ScaleValue, ScaleValue);
        }

        [RelayCommand]
        private void ScaleXAxis()
        {
            ScaleModel(ScaleX, 1, 1);
        }

        [RelayCommand]
        private void ScaleYAxis()
        {
            ScaleModel(1, ScaleY, 1);
        }

        [RelayCommand]
        private void ScaleZAxis()
        {
            ScaleModel(1, 1, ScaleZ);
        }

        private void ScaleModel(double scaleX, double scaleY, double scaleZ)
        {
            if (CurrentModel == null || currentModelVisual == null) return;

            var currentTransform = CurrentModel.Transform as Transform3DGroup ?? new Transform3DGroup();

            // Get current bounds to scale around current center
            var bounds = GetTransformedBounds();
            var scaleCenter = new Point3D(
                bounds.X + bounds.SizeX / 2,
                bounds.Y + bounds.SizeY / 2,
                bounds.Z + bounds.SizeZ / 2
            );

            var scaleTransform = new ScaleTransform3D(
                scaleX, scaleY, scaleZ,
                scaleCenter.X,
                scaleCenter.Y,
                scaleCenter.Z
            );

            currentTransform.Children.Add(scaleTransform);
            CurrentModel.Transform = currentTransform;
            UpdateModelVisual();

            StatusMessage = $"Scaled: X{scaleX:0.##} Y{scaleY:0.##} Z{scaleZ:0.##}";
        }

        private void UpdateModelVisual()
        {
            if (currentModelVisual != null && CurrentModel != null)
            {
                currentModelVisual.Transform = CurrentModel.Transform;
            }
        }

        [RelayCommand]
        private void SliceModel()
        {
            if (CurrentModel == null)
            {
                MessageBox.Show("Please load a model first.", "No Model",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                StatusMessage = "Slicing model...";

                if (PrinterSettings.LayerThickness <= 0 || PrinterSettings.LayerThickness > 1)
                {
                    MessageBox.Show("Layer thickness must be between 0.01 and 1.0 mm",
                        "Invalid Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                    StatusMessage = "Invalid layer thickness";
                    return;
                }

                var sliceResult = slicingEngine.SliceModel(CurrentModel, PrinterSettings);

                StatusMessage = $"Slicing complete: {sliceResult.Layers.Count} layers generated";

                MessageBox.Show($"Slicing completed successfully!\n\n" +
                    $"Layers: {sliceResult.Layers.Count}\n" +
                    $"Print Time: {sliceResult.PrintTime:F1} minutes\n" +
                    $"Exposure Time: {sliceResult.TotalExposureTime:F0}s\n" +
                    $"Lift Time: {sliceResult.TotalLiftTime:F0}s\n" +
                    $"Model Height: {CurrentModel.Size.Z:F2} mm\n" +
                    $"Layer Thickness: {PrinterSettings.LayerThickness:F3} mm\n" +
                    $"Estimated Resin: {sliceResult.EstimatedResinVolume:F1} ml\n" +
                    $"Estimated Cost: ${sliceResult.EstimatedCost:F2}",
                    "Slicing Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (OutOfMemoryException)
            {
                MessageBox.Show("Out of memory error during slicing.\n\n" +
                    "Try increasing the layer thickness or reducing the model size.",
                    "Memory Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Slicing failed - Out of memory";
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Slicing Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Slicing failed";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during slicing: {ex.Message}", "Slicing Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Slicing failed";
            }
        }

        [RelayCommand]
        private void SaveProfile()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Printer Profile (*.json)|*.json",
                FileName = $"{PrinterSettings.ProfileName}.json",
                Title = "Save Printer Profile"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    ProfileManager.SaveProfile(PrinterSettings, dialog.FileName);
                    StatusMessage = $"Profile saved: {Path.GetFileName(dialog.FileName)}";
                    MessageBox.Show("Profile saved successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving profile: {ex.Message}", "Save Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusMessage = "Failed to save profile";
                }
            }
        }

        [RelayCommand]
        private void LoadProfile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Printer Profile (*.json)|*.json",
                Title = "Load Printer Profile"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var settings = ProfileManager.LoadProfile(dialog.FileName);
                    if (settings != null)
                    {
                        PrinterSettings = settings;
                        StatusMessage = $"Profile loaded: {Path.GetFileName(dialog.FileName)}";
                        MessageBox.Show("Profile loaded successfully!", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to load profile. File may be corrupted.",
                            "Load Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        StatusMessage = "Failed to load profile";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading profile: {ex.Message}", "Load Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusMessage = "Failed to load profile";
                }
            }
        }

        [RelayCommand]
        private void CreateProfile()
        {
            var newProfileName = Microsoft.VisualBasic.Interaction.InputBox("Enter new profile name:", "Create Profile", "NewProfile");
            if (!string.IsNullOrEmpty(newProfileName))
            {
                ProfileManager.CreateProfile(newProfileName);
                LoadAvailableProfiles();
                SelectedProfile = newProfileName;
                PrinterSettings.ProfileName = newProfileName;
                StatusMessage = $"Profile created: {newProfileName}";
            }
        }

        [RelayCommand]
        private void DeleteProfile()
        {
            if (SelectedProfile == "default")
            {
                MessageBox.Show("Cannot delete the default profile.", "Delete Profile",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to delete profile '{SelectedProfile}'?",
                "Delete Profile", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ProfileManager.DeleteProfile(SelectedProfile);
                LoadAvailableProfiles();
                SelectedProfile = "default";
                StatusMessage = $"Profile deleted: {SelectedProfile}";
            }
        }

        [RelayCommand]
        private void ResetView()
        {
            if (Viewport3D == null) return;
            Viewport3D.ResetCamera();
            Viewport3D.ZoomExtents(500);
            StatusMessage = "View reset";
        }

        [RelayCommand]
        private void TopView()
        {
            if (CurrentModel == null || Viewport3D == null) return;

            var bounds = GetTransformedBounds();
            var lookDirection = new Vector3D(0, 0, -1);
            var upDirection = new Vector3D(0, 1, 0);
            var cameraDistance = Math.Max(bounds.SizeX, bounds.SizeY) * 2;

            Viewport3D.SetView(
                new Point3D(0, 0, cameraDistance),
                lookDirection,
                upDirection,
                500);

            StatusMessage = "Top view";
        }

        [RelayCommand]
        private void FrontView()
        {
            if (CurrentModel == null || Viewport3D == null) return;

            var bounds = GetTransformedBounds();
            var lookDirection = new Vector3D(0, -1, 0);
            var upDirection = new Vector3D(0, 0, 1);
            var cameraDistance = Math.Max(bounds.SizeX, bounds.SizeZ) * 2;

            Viewport3D.SetView(
                new Point3D(0, cameraDistance, bounds.SizeZ / 2),
                lookDirection,
                upDirection,
                500);

            StatusMessage = "Front view";
        }

        [RelayCommand]
        private void SideView()
        {
            if (CurrentModel == null || Viewport3D == null) return;

            var bounds = GetTransformedBounds();
            var lookDirection = new Vector3D(-1, 0, 0);
            var upDirection = new Vector3D(0, 0, 1);
            var cameraDistance = Math.Max(bounds.SizeY, bounds.SizeZ) * 2;

            Viewport3D.SetView(
                new Point3D(cameraDistance, 0, bounds.SizeZ / 2),
                lookDirection,
                upDirection,
                500);

            StatusMessage = "Side view";
        }

        partial void OnSelectedProfileChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                PrinterSettings.ProfileName = value;
            }
        }
    }
}