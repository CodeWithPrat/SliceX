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
using SliceX.Export;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using SliceX.Views;

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

        // Navigation Properties
        [ObservableProperty]
        private UserControl currentView;

        [ObservableProperty]
        private string generatedGCode = "";

        [ObservableProperty]
        private BitmapSource currentLayerImage;

        [ObservableProperty]
        private int currentLayerNumber = 1;

        [ObservableProperty]
        private int totalLayers = 0;

        // Machine Control Properties
        [ObservableProperty]
        private double zRateFast = 3000;

        [ObservableProperty]
        private double zRateSlow = 100;

        [ObservableProperty]
        private string gCodeToSend = "";

        [ObservableProperty]
        private string sentGCode = "";

        [ObservableProperty]
        private double extrudeDistance0 = 10;

        [ObservableProperty]
        private double reverseRate0 = 100;

        [ObservableProperty]
        private double extrudeDistance1 = 10;

        [ObservableProperty]
        private double reverseRate1 = 100;

        [ObservableProperty]
        private string projectorCommands = "";

        // Machine Config Properties
        [ObservableProperty]
        private ObservableCollection<string> machineProfiles = new ObservableCollection<string>
        {
            "NullMachine", "Default_FDM", "Default_SLA", "SUKSHM3D"
        };

        [ObservableProperty]
        private string selectedMachineProfile = "NullMachine";

        [ObservableProperty]
        private ObservableCollection<string> allMachineProfiles = new ObservableCollection<string>
        {
            "Default_FDM", "Default_SLA", "NullMachine", "SUKSHM3D"
        };

        [ObservableProperty]
        private ObservableCollection<string> machineTypes = new ObservableCollection<string>
        {
            "UV_DLP", "FDM", "SLA", "DLP"
        };

        [ObservableProperty]
        private string selectedMachineType = "UV_DLP";

        [ObservableProperty]
        private double xAxisLength = 14.515;

        [ObservableProperty]
        private double xAxisFeedRate = 100;

        [ObservableProperty]
        private string xAxisDriver = "eNULL_DRIVER";

        [ObservableProperty]
        private string xAxisConnection = "eRF_3DLPRINTER";

        [ObservableProperty]
        private double yAxisLength = 8.165;

        [ObservableProperty]
        private double yAxisFeedRate = 100;

        [ObservableProperty]
        private string yAxisDriver = "EGENERIC";

        [ObservableProperty]
        private string yAxisConnection = "eRF_3DLPRINTER";

        [ObservableProperty]
        private ObservableCollection<string> drivers = new ObservableCollection<string>
        {
            "eNULL_DRIVER", "EGENERIC", "eRF_3DLPRINTER"
        };

        [ObservableProperty]
        private ObservableCollection<string> connectionTypes = new ObservableCollection<string>
        {
            "eRF_3DLPRINTER", "SERIAL", "ETHERNET"
        };

        [ObservableProperty]
        private ObservableCollection<string> displayDevices = new ObservableCollection<string>
        {
            @"V:\DISPLAY1", @"V:\DISPLAY2"
        };

        [ObservableProperty]
        private string selectedDisplayDevice = @"V:\DISPLAY1";

        [ObservableProperty]
        private int displayWidth = 1920;

        [ObservableProperty]
        private int displayHeight = 1080;

        [ObservableProperty]
        private bool projectorSerialEnabled = false;

        // Slice Profile Properties
        [ObservableProperty]
        private ObservableCollection<string> sliceProfiles = new ObservableCollection<string>
        {
            "default", "3dlnk_Acrylic", "HT", "PMMA"
        };

        [ObservableProperty]
        private string selectedSliceProfile = "default";

        [ObservableProperty]
        private ObservableCollection<string> allSliceProfiles = new ObservableCollection<string>
        {
            "3dlnk_Acrylic", "default", "HT", "PMMA"
        };

        [ObservableProperty]
        private bool exportToZip = true;

        [ObservableProperty]
        private bool exportToDirectory = false;

        [ObservableProperty]
        private bool useMainLiftGCode = false;

        [ObservableProperty]
        private bool autoCalcLiftTime = false;

        [ObservableProperty]
        private ObservableCollection<string> buildDirections = new ObservableCollection<string>
        {
            "Bottom_Up", "Top_Down"
        };

        private HelixViewport3D? viewport3D;
        private SliceResult? currentSliceResult;
        private readonly SliceX.Utilities.ModelImporter modelImporter = new SliceX.Utilities.ModelImporter();
        private readonly SlicingEngine slicingEngine = new SlicingEngine();
        private ModelVisual3D? currentModelVisual;

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

        // Add these private fields to MainViewModel
        private ThreeDView _threeDView;
        private GCodeView _gCodeView;
        private SliceViewerView _sliceViewerView;
        private MachineControlView _machineControlView;
        private MachineConfigView _machineConfigView;
        private SliceProfileView _sliceProfileView;

        // Update the constructor
        public MainViewModel()
        {
            LoadAvailableProfiles();

            // Initialize all views once and cache them
            InitializeViews();

            // Set default view
            CurrentView = _threeDView;
        }

        private void InitializeViews()
        {
            _threeDView = new ThreeDView { DataContext = this };
            _gCodeView = new GCodeView { DataContext = this };
            _sliceViewerView = new SliceViewerView { DataContext = this };
            _machineControlView = new MachineControlView { DataContext = this };
            _machineConfigView = new MachineConfigView { DataContext = this };
            _sliceProfileView = new SliceProfileView { DataContext = this };
        }

        // Update navigation commands to use cached views
        [RelayCommand]
        private void NavigateTo3DView()
        {
            CurrentView = _threeDView;
            if (_threeDView is ThreeDView threeDView)
            {
                threeDView.OnActivated();
            }
        }

        [RelayCommand]
        private void NavigateToGCode()
        {
            CurrentView = _gCodeView;
            if (_gCodeView is GCodeView gCodeView)
            {
                gCodeView.OnActivated();
            }
        }

        [RelayCommand]
        private void NavigateToSliceViewer() => CurrentView = _sliceViewerView;

        [RelayCommand]
        private void NavigateToMachineControl() => CurrentView = _machineControlView;

        [RelayCommand]
        private void NavigateToMachineConfig() => CurrentView = _machineConfigView;

        [RelayCommand]
        private void NavigateToSliceProfile() => CurrentView = _sliceProfileView;

        // New Commands for additional functionality
        [RelayCommand]
        private void CopyGCode()
        {
            if (!string.IsNullOrEmpty(GeneratedGCode))
            {
                Clipboard.SetText(GeneratedGCode);
                StatusMessage = "G-Code copied to clipboard!";
            }
        }

        [RelayCommand]
        private void EstimateVolumeAndCost()
        {
            if (CurrentModel == null)
            {
                MessageBox.Show("Please load a model first.", "No Model",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var tempResult = slicingEngine.SliceModel(CurrentModel, PrinterSettings);
                MessageBox.Show(
                    $"Estimated Resin Volume: {tempResult.EstimatedResinVolume:F1} ml\n" +
                    $"Estimated Cost: ${tempResult.EstimatedCost:F2}",
                    "Volume and Cost Estimate",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error estimating volume: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ShowPreferences()
        {
            // Implementation for preferences dialog
            MessageBox.Show("Preferences dialog will be implemented here.", "Preferences",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void Exit()
        {
            Application.Current.Shutdown();
        }

        [RelayCommand]
        private void FirstLayer()
        {
            if (currentSliceResult?.Layers.Count > 0)
            {
                CurrentLayerNumber = 1;
                UpdateLayerImage();
            }
        }

        [RelayCommand]
        private void PreviousLayer()
        {
            if (CurrentLayerNumber > 1)
            {
                CurrentLayerNumber--;
                UpdateLayerImage();
            }
        }

        [RelayCommand]
        private void NextLayer()
        {
            if (CurrentLayerNumber < TotalLayers)
            {
                CurrentLayerNumber++;
                UpdateLayerImage();
            }
        }

        [RelayCommand]
        private void LastLayer()
        {
            if (currentSliceResult?.Layers.Count > 0)
            {
                CurrentLayerNumber = TotalLayers;
                UpdateLayerImage();
            }
        }

        // Machine Control Commands
        [RelayCommand]
        private void EnableMotors() => StatusMessage = "Motors enabled";

        [RelayCommand]
        private void DisableMotors() => StatusMessage = "Motors disabled";

        [RelayCommand]
        private void SendGCode()
        {
            if (!string.IsNullOrEmpty(GCodeToSend))
            {
                SentGCode = GCodeToSend;
                StatusMessage = "G-Code sent to machine";
                GCodeToSend = "";
            }
        }

        [RelayCommand]
        private void ExtrudeTool0() => StatusMessage = $"Extruded {ExtrudeDistance0}mm on Tool 0";

        [RelayCommand]
        private void ReverseTool0() => StatusMessage = $"Reversed {ReverseRate0}mm/min on Tool 0";

        [RelayCommand]
        private void ExtrudeTool1() => StatusMessage = $"Extruded {ExtrudeDistance1}mm on Tool 1";

        [RelayCommand]
        private void ReverseTool1() => StatusMessage = $"Reversed {ReverseRate1}mm/min on Tool 1";

        [RelayCommand]
        private void HomeX() => StatusMessage = "Homing X axis";

        [RelayCommand]
        private void HomeY() => StatusMessage = "Homing Y axis";

        [RelayCommand]
        private void HomeZ() => StatusMessage = "Homing Z axis";

        [RelayCommand]
        private void HomeAll() => StatusMessage = "Homing all axes";

        [RelayCommand]
        private void ShowProjector() => StatusMessage = "Projector shown";

        [RelayCommand]
        private void ConnectMonitor() => StatusMessage = "Monitor connected";

        [RelayCommand]
        private void ShowBlank() => StatusMessage = "Blank screen shown";

        [RelayCommand]
        private void EditCommands() => StatusMessage = "Editing commands";

        [RelayCommand]
        private void HideProjector() => StatusMessage = "Projector hidden";

        [RelayCommand]
        private void SendProjector() => StatusMessage = "Projector command sent";

        [RelayCommand]
        private void ClearProjector() => ProjectorCommands = "";

        // Machine Config Commands
        [RelayCommand]
        private void CreateMachineProfile()
        {
            var newName = Microsoft.VisualBasic.Interaction.InputBox("Enter new machine profile name:", "Create Profile", "NewMachine");
            if (!string.IsNullOrEmpty(newName))
            {
                MachineProfiles.Add(newName);
                AllMachineProfiles.Add(newName);
                SelectedMachineProfile = newName;
                StatusMessage = $"Machine profile created: {newName}";
            }
        }

        [RelayCommand]
        private void DeleteMachineProfile()
        {
            if (SelectedMachineProfile != "NullMachine")
            {
                MachineProfiles.Remove(SelectedMachineProfile);
                AllMachineProfiles.Remove(SelectedMachineProfile);
                SelectedMachineProfile = "NullMachine";
                StatusMessage = "Machine profile deleted";
            }
        }

        [RelayCommand]
        private void ConfigureXAxis() => StatusMessage = "Configuring X axis";

        [RelayCommand]
        private void ConfigureYAxis() => StatusMessage = "Configuring Y axis";

        [RelayCommand]
        private void RefreshDisplays() => StatusMessage = "Displays refreshed";

        [RelayCommand]
        private void ConfigureProjector() => StatusMessage = "Configuring projector";

        [RelayCommand]
        private void SaveMachineConfig() => StatusMessage = "Machine configuration saved";

        // Slice Profile Commands
        [RelayCommand]
        private void CreateSliceProfile()
        {
            var newName = Microsoft.VisualBasic.Interaction.InputBox("Enter new slice profile name:", "Create Profile", "NewProfile");
            if (!string.IsNullOrEmpty(newName))
            {
                SliceProfiles.Add(newName);
                AllSliceProfiles.Add(newName);
                SelectedSliceProfile = newName;
                PrinterSettings.ProfileName = newName;
                StatusMessage = $"Slice profile created: {newName}";
            }
        }

        [RelayCommand]
        private void DeleteSliceProfile()
        {
            if (SelectedSliceProfile != "default")
            {
                SliceProfiles.Remove(SelectedSliceProfile);
                AllSliceProfiles.Remove(SelectedSliceProfile);
                SelectedSliceProfile = "default";
                PrinterSettings.ProfileName = "default";
                StatusMessage = "Slice profile deleted";
            }
        }

        [RelayCommand]
        private void ApplySliceProfile() => StatusMessage = "Slice profile applied";

        private void UpdateLayerImage()
        {
            if (currentSliceResult?.Layers.Count >= CurrentLayerNumber && CurrentLayerNumber > 0)
            {
                var layer = currentSliceResult.Layers[CurrentLayerNumber - 1];
                if (layer.ImageData != null && layer.ImageData.Length > 0)
                {
                    var bitmap = new BitmapImage();
                    using (var stream = new MemoryStream(layer.ImageData))
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                    }
                    CurrentLayerImage = bitmap;
                }
            }
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

            try
            {
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
            catch (Exception ex)
            {
                // Log the error but don't crash the application
                System.Diagnostics.Debug.WriteLine($"Error initializing viewport: {ex.Message}");
                StatusMessage = "Viewport initialization completed with warnings";
            }
        }

        private void ConfigureMouseGestures()
        {
            if (Viewport3D == null) return;

            try
            {
                // Configure mouse gestures for optimal control
                Viewport3D.RotateGesture = new MouseGesture(MouseAction.LeftClick);
                Viewport3D.PanGesture = new MouseGesture(MouseAction.RightClick);
                Viewport3D.ZoomGesture = new MouseGesture(MouseAction.MiddleClick);
                Viewport3D.ZoomGesture2 = new MouseGesture(MouseAction.WheelClick);

                // IMPORTANT FIX: Do not set gestures to null as it causes ArgumentNullException
                // Instead, disable the functionality through other properties
                Viewport3D.IsChangeFieldOfViewEnabled = false; // This disables ChangeFieldOfViewGesture

                // For ZoomRectangleGesture, we don't set it to null, we just don't assign it
                // The default behavior will be used

                // Fine-tune camera controller settings
                if (Viewport3D.CameraController != null)
                {
                    Viewport3D.CameraController.CameraRotationMode = CameraRotationMode.Trackball;
                    Viewport3D.CameraController.InfiniteSpin = false;
                    Viewport3D.CameraController.RotationSensitivity = RotationSensitivity;
                    Viewport3D.CameraController.ZoomSensitivity = ZoomSensitivity;
                    Viewport3D.CameraController.UpDownPanSensitivity = PanSensitivity;
                    Viewport3D.CameraController.LeftRightPanSensitivity = PanSensitivity;
                    Viewport3D.CameraController.IsInertiaEnabled = true;

                    // Additional camera controller optimizations
                    Viewport3D.CameraController.RotateAroundMouseDownPoint = true;
                    Viewport3D.CameraController.ZoomAroundMouseDownPoint = true;
                    Viewport3D.CameraController.ShowCameraTarget = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configuring mouse gestures: {ex.Message}");
                // Fallback to default gestures if custom configuration fails
                try
                {
                    // Reset to default gestures
                    Viewport3D.RotateGesture = new MouseGesture(MouseAction.LeftClick);
                    Viewport3D.PanGesture = new MouseGesture(MouseAction.RightClick);
                    Viewport3D.ZoomGesture = new MouseGesture(MouseAction.MiddleClick);
                }
                catch
                {
                    // If even defaults fail, just continue with whatever works
                }
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
                        Width = printerSettings.BuildVolumeX * 0.5,
                        Length = printerSettings.BuildVolumeY,
                        Height = 1, // thinner plate
                        Center = new Point3D(0, 0, -0.5),
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
                    var modelMaterial = new MaterialGroup();

                    // Base diffuse color (deep PCB green)
                    var diffuse = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0, 80, 0)));
                    modelMaterial.Children.Add(diffuse);

                    // Add a slight specular shine (for realistic PCB reflection)
                    var specular = new SpecularMaterial(new SolidColorBrush(Color.FromRgb(50, 200, 50)), 20);
                    modelMaterial.Children.Add(specular);

                    // Optional: slight emissive tint for subtle glow
                    var emissive = new EmissiveMaterial(new SolidColorBrush(Color.FromRgb(0, 40, 0)));
                    modelMaterial.Children.Add(emissive);

                    // Back material (lighter green for contrast)
                    var backMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0, 100, 0)));

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
        private async void SliceModel()
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

                // Perform slicing
                currentSliceResult = slicingEngine.SliceModel(CurrentModel, PrinterSettings);

                // Generate GCode using the GCodeGenerator
                var gcodeGenerator = new GCodeGenerator();
                GeneratedGCode = gcodeGenerator.GenerateGCode(currentSliceResult, PrinterSettings);

                // Update slice viewer properties
                TotalLayers = currentSliceResult.Layers.Count;
                CurrentLayerNumber = 1;
                UpdateLayerImage();

                StatusMessage = $"Slicing complete: {currentSliceResult.Layers.Count} layers generated";

                // Ask user if they want to export
                var result = MessageBox.Show(
                    $"Slicing completed successfully!\n\n" +
                    $"Layers: {currentSliceResult.Layers.Count}\n" +
                    $"Print Time: {currentSliceResult.PrintTime:F1} minutes\n" +
                    $"Exposure Time: {currentSliceResult.TotalExposureTime:F0}s\n" +
                    $"Lift Time: {currentSliceResult.TotalLiftTime:F0}s\n" +
                    $"Model Height: {CurrentModel.Size.Z:F2} mm\n" +
                    $"Layer Thickness: {PrinterSettings.LayerThickness:F3} mm\n" +
                    $"Estimated Resin: {currentSliceResult.EstimatedResinVolume:F1} ml\n" +
                    $"Estimated Cost: ${currentSliceResult.EstimatedCost:F2}\n\n" +
                    $"Do you want to export G-code and layer images to a ZIP file?",
                    "Slicing Complete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Show save dialog
                    var saveDialog = new SaveFileDialog
                    {
                        Filter = "ZIP Archive (*.zip)|*.zip",
                        FileName = $"{Path.GetFileNameWithoutExtension(CurrentModel.FileName)}_sliced.zip",
                        Title = "Save Sliced Output"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        // Create progress window
                        var progressWindow = new Window
                        {
                            Title = "Exporting...",
                            Width = 400,
                            Height = 150,
                            WindowStartupLocation = WindowStartupLocation.CenterScreen,
                            ResizeMode = ResizeMode.NoResize,
                            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30))
                        };

                        var stackPanel = new StackPanel
                        {
                            Margin = new Thickness(20)
                        };

                        var statusText = new TextBlock
                        {
                            Text = "Generating G-code and layer images...",
                            Foreground = Brushes.White,
                            FontSize = 14,
                            Margin = new Thickness(0, 0, 0, 10)
                        };

                        var progressBar = new System.Windows.Controls.ProgressBar
                        {
                            Height = 25,
                            Minimum = 0,
                            Maximum = 100,
                            Value = 0
                        };

                        var progressText = new TextBlock
                        {
                            Text = "0%",
                            Foreground = Brushes.White,
                            FontSize = 12,
                            Margin = new Thickness(0, 10, 0, 0),
                            HorizontalAlignment = HorizontalAlignment.Center
                        };

                        stackPanel.Children.Add(statusText);
                        stackPanel.Children.Add(progressBar);
                        stackPanel.Children.Add(progressText);
                        progressWindow.Content = stackPanel;

                        progressWindow.Show();

                        try
                        {
                            StatusMessage = "Exporting to ZIP...";

                            // Export in background
                            var exporter = new SliceExporter();
                            var progress = new Progress<int>(percent =>
                            {
                                progressBar.Value = percent;
                                progressText.Text = $"{percent}%";
                                statusText.Text = percent < 100
                                    ? $"Generating layer images... ({percent}%)"
                                    : "Finalizing ZIP file...";
                            });

                            await Task.Run(() =>
                            {
                                exporter.ExportToZip(CurrentModel, currentSliceResult, PrinterSettings,
                                                    saveDialog.FileName, progress);
                            });

                            progressWindow.Close();

                            StatusMessage = "Export complete!";
                            MessageBox.Show(
                                $"Export completed successfully!\n\n" +
                                $"Output saved to:\n{saveDialog.FileName}\n\n" +
                                $"The ZIP contains:\n" +
                                $"• output.gcode - G-code file\n" +
                                $"• layers/ - {currentSliceResult.TotalLayers} layer images\n" +
                                $"• metadata.txt - Print information",
                                "Export Complete",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            progressWindow.Close();
                            MessageBox.Show($"Error during export: {ex.Message}", "Export Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            StatusMessage = "Export failed";
                        }
                    }
                }
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
        private void SaveGCode()
        {
            if (string.IsNullOrEmpty(GeneratedGCode))
            {
                MessageBox.Show("No G-Code to save. Please slice a model first.", "No G-Code",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "G-Code Files (*.gcode)|*.gcode|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                FileName = $"{Path.GetFileNameWithoutExtension(CurrentModel?.FileName ?? "model")}.gcode",
                Title = "Save G-Code File"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(saveDialog.FileName, GeneratedGCode);
                    StatusMessage = $"G-Code saved to: {Path.GetFileName(saveDialog.FileName)}";
                    MessageBox.Show("G-Code saved successfully!", "Save Complete",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving G-Code: {ex.Message}", "Save Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusMessage = "Failed to save G-Code";
                }
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

        // Add these properties to MainViewModel
        [ObservableProperty]
        private bool isDragOver;

        [ObservableProperty]
        private string dragDropMessage = "Drop 3D model file here";

        // Add these commands to MainViewModel
        [RelayCommand]
        private void UnloadModel()
        {
            if (Viewport3D == null) return;

            try
            {
                // Clear the model from viewport
                Viewport3D.Children.Clear();

                // Reset model-related properties
                CurrentModel = null;
                IsModelLoaded = false;
                currentModelVisual = null;

                // Clear slice results
                currentSliceResult = null;
                GeneratedGCode = "";
                TotalLayers = 0;
                CurrentLayerNumber = 0;
                CurrentLayerImage = null;

                // Reinitialize viewport with default elements
                InitializeViewport();

                StatusMessage = "Model unloaded";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error unloading model: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Error unloading model";
            }
        }

        [RelayCommand]
        private void HandleDragEnter(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string extension = Path.GetExtension(files[0]).ToLower();
                    if (extension == ".stl" || extension == ".obj")
                    {
                        IsDragOver = true;
                        DragDropMessage = $"Load {Path.GetFileName(files[0])}";
                        e.Effects = DragDropEffects.Copy;
                        e.Handled = true;
                        return;
                    }
                }
            }
            IsDragOver = false;
            DragDropMessage = "Drop 3D model file here";
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        [RelayCommand]
        private void HandleDragLeave()
        {
            IsDragOver = false;
            DragDropMessage = "Drop 3D model file here";
        }

        [RelayCommand]
        private void HandleDrop(DragEventArgs e)
        {
            IsDragOver = false;
            DragDropMessage = "Drop 3D model file here";

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string filePath = files[0];
                    string extension = Path.GetExtension(filePath).ToLower();

                    if (extension == ".stl" || extension == ".obj")
                    {
                        // Use existing LoadModel logic but with the dropped file
                        LoadModelFromFile(filePath);
                    }
                    else
                    {
                        MessageBox.Show("Please drop a valid 3D model file (.stl or .obj)", "Invalid File",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            e.Handled = true;
        }

        private void LoadModelFromFile(string filePath)
        {
            if (Viewport3D == null) return;

            try
            {
                StatusMessage = "Loading model...";
                CurrentModel = modelImporter.ImportModel(filePath);

                if (CurrentModel == null)
                {
                    StatusMessage = "Failed to load model";
                    return;
                }

                // Clear previous model and reinitialize viewport
                Viewport3D.Children.Clear();
                InitializeViewport(); // This will add lights, grid, etc.

                // Create material for the model (same as in LoadModel)
                var modelMaterial = new MaterialGroup();
                var diffuse = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0, 80, 0)));
                var specular = new SpecularMaterial(new SolidColorBrush(Color.FromRgb(50, 200, 50)), 20);
                var emissive = new EmissiveMaterial(new SolidColorBrush(Color.FromRgb(0, 40, 0)));
                modelMaterial.Children.Add(diffuse);
                modelMaterial.Children.Add(specular);
                modelMaterial.Children.Add(emissive);

                var backMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0, 100, 0)));

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
                StatusMessage = $"Model loaded: {Path.GetFileName(filePath)} ({CurrentModel.Triangles.Count} triangles)";

                // Add to recent files
                if (!RecentFiles.Contains(filePath))
                {
                    RecentFiles.Insert(0, filePath);
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
}
