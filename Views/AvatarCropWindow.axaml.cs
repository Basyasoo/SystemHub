using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace SystemHub.Views
{
    public partial class AvatarCropWindow : Window
    {
        public static readonly StyledProperty<Bitmap?> SourceImageProperty =
            AvaloniaProperty.Register<AvatarCropWindow, Bitmap?>(nameof(SourceImage));

        public Bitmap? SourceImage
        {
            get => GetValue(SourceImageProperty);
            set => SetValue(SourceImageProperty, value);
        }

        public static readonly StyledProperty<double> ImageRenderWidthProperty =
            AvaloniaProperty.Register<AvatarCropWindow, double>(nameof(ImageRenderWidth));

        public double ImageRenderWidth
        {
            get => GetValue(ImageRenderWidthProperty);
            set => SetValue(ImageRenderWidthProperty, value);
        }

        public static readonly StyledProperty<double> ImageRenderHeightProperty =
            AvaloniaProperty.Register<AvatarCropWindow, double>(nameof(ImageRenderHeight));

        public double ImageRenderHeight
        {
            get => GetValue(ImageRenderHeightProperty);
            set => SetValue(ImageRenderHeightProperty, value);
        }

        public static readonly StyledProperty<double> TransformScaleXProperty =
            AvaloniaProperty.Register<AvatarCropWindow, double>(nameof(TransformScaleX), 1.0);

        public double TransformScaleX
        {
            get => GetValue(TransformScaleXProperty);
            set => SetValue(TransformScaleXProperty, value);
        }

        public static readonly StyledProperty<double> TransformScaleYProperty =
            AvaloniaProperty.Register<AvatarCropWindow, double>(nameof(TransformScaleY), 1.0);

        public double TransformScaleY
        {
            get => GetValue(TransformScaleYProperty);
            set => SetValue(TransformScaleYProperty, value);
        }

        public static readonly StyledProperty<double> TransformAngleProperty =
            AvaloniaProperty.Register<AvatarCropWindow, double>(nameof(TransformAngle));

        public double TransformAngle
        {
            get => GetValue(TransformAngleProperty);
            set => SetValue(TransformAngleProperty, value);
        }

        public static readonly StyledProperty<double> TransformXProperty =
            AvaloniaProperty.Register<AvatarCropWindow, double>(nameof(TransformX));

        public double TransformX
        {
            get => GetValue(TransformXProperty);
            set => SetValue(TransformXProperty, value);
        }

        public static readonly StyledProperty<double> TransformYProperty =
            AvaloniaProperty.Register<AvatarCropWindow, double>(nameof(TransformY));

        public double TransformY
        {
            get => GetValue(TransformYProperty);
            set => SetValue(TransformYProperty, value);
        }

        public static readonly StyledProperty<double> ZoomScaleProperty =
            AvaloniaProperty.Register<AvatarCropWindow, double>(nameof(ZoomScale), 1.0);

        public double ZoomScale
        {
            get => GetValue(ZoomScaleProperty);
            set => SetValue(ZoomScaleProperty, value);
        }

        private bool _isFlippedH;
        private bool _isFlippedV;

        private bool _isDraggingImage;
        private Point _lastPointerPosition;
        private double _startTranslateX;
        private double _startTranslateY;

        public string? ResultPath { get; private set; }

        public AvatarCropWindow()
        {
            DataContext = this;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            var viewport = this.FindControl<Grid>("ViewportContainer");
            if (viewport != null)
            {
                viewport.PointerPressed += OnViewportPointerPressed;
                viewport.PointerMoved += OnViewportPointerMoved;
                viewport.PointerReleased += OnViewportPointerReleased;
                viewport.PointerWheelChanged += OnViewportPointerWheelChanged;
            }

            SourceImageProperty.Changed.AddClassHandler<AvatarCropWindow>((x, e) => x.OnSourceImageChanged(e));
            ZoomScaleProperty.Changed.AddClassHandler<AvatarCropWindow>((x, e) => x.OnZoomScaleChanged(e));
        }

        private void OnSourceImageChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (SourceImage != null)
            {
                double imgWidth = SourceImage.Size.Width;
                double imgHeight = SourceImage.Size.Height;
                double fitScale = Math.Min(300.0 / imgWidth, 300.0 / imgHeight);

                ImageRenderWidth = imgWidth * fitScale;
                ImageRenderHeight = imgHeight * fitScale;

                ZoomScale = 1.0;
                TransformX = 0;
                TransformY = 0;
                TransformAngle = 0;
                _isFlippedH = false;
                _isFlippedV = false;

                UpdateScaleTransforms();
            }
        }

        private void OnZoomScaleChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdateScaleTransforms();
        }

        private void UpdateScaleTransforms()
        {
            TransformScaleX = (_isFlippedH ? -1.0 : 1.0) * ZoomScale;
            TransformScaleY = (_isFlippedV ? -1.0 : 1.0) * ZoomScale;
        }

        private void OnViewportPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var properties = e.GetCurrentPoint(sender as Visual).Properties;
            if (properties.IsLeftButtonPressed)
            {
                _isDraggingImage = true;
                _lastPointerPosition = e.GetPosition(sender as Visual);
                _startTranslateX = TransformX;
                _startTranslateY = TransformY;
                e.Handled = true;
            }
        }

        private void OnViewportPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_isDraggingImage)
            {
                var currentPosition = e.GetPosition(sender as Visual);
                var delta = currentPosition - _lastPointerPosition;

                TransformX = _startTranslateX + delta.X;
                TransformY = _startTranslateY + delta.Y;
                e.Handled = true;
            }
        }

        private void OnViewportPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isDraggingImage)
            {
                _isDraggingImage = false;
                e.Handled = true;
            }
        }

        private void OnViewportPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            double newZoom = ZoomScale + e.Delta.Y * 0.1;
            ZoomScale = Math.Clamp(newZoom, 1.0, 4.0);
            e.Handled = true;
        }

        private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
            }
        }

        private void OnRotateLeftClick(object? sender, RoutedEventArgs e)
        {
            TransformAngle = (TransformAngle - 90) % 360;
        }

        private void OnRotateRightClick(object? sender, RoutedEventArgs e)
        {
            TransformAngle = (TransformAngle + 90) % 360;
        }

        private void OnFlipHorizontalClick(object? sender, RoutedEventArgs e)
        {
            _isFlippedH = !_isFlippedH;
            UpdateScaleTransforms();
        }

        private void OnFlipVerticalClick(object? sender, RoutedEventArgs e)
        {
            _isFlippedV = !_isFlippedV;
            UpdateScaleTransforms();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            ResultPath = null;
            Close();
        }

        private void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            if (SourceImage == null)
            {
                Close();
                return;
            }

            try
            {
                // Create a virtual Grid replicating the UI viewport layout
                var renderGrid = new Grid
                {
                    Width = 300,
                    Height = 300,
                    Background = Brushes.Transparent
                };

                var imageControl = new Image
                {
                    Source = SourceImage,
                    Width = ImageRenderWidth,
                    Height = ImageRenderHeight,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                // Replicate exact render transformations
                var transformGroup = new TransformGroup();
                transformGroup.Children.Add(new ScaleTransform(TransformScaleX, TransformScaleY));
                transformGroup.Children.Add(new RotateTransform(TransformAngle));
                transformGroup.Children.Add(new TranslateTransform(TransformX, TransformY));

                imageControl.RenderTransform = transformGroup;
                renderGrid.Children.Add(imageControl);

                // Force layout generation
                renderGrid.Measure(new Size(300, 300));
                renderGrid.Arrange(new Rect(0, 0, 300, 300));

                // Render 300x300 viewport to bitmap
                using var fullBitmap = new RenderTargetBitmap(new PixelSize(300, 300));
                fullBitmap.Render(renderGrid);

                // Crop the center 240x240 circular crop area
                using var croppedBitmap = new RenderTargetBitmap(new PixelSize(240, 240));
                using (var ctx = croppedBitmap.CreateDrawingContext())
                {
                    ctx.DrawImage(
                        fullBitmap,
                        new Rect(30, 30, 240, 240),
                        new Rect(0, 0, 240, 240)
                    );
                }

                // Save to temp file
                string tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp_avatars");
                Directory.CreateDirectory(tempDir);
                string tempFile = Path.Combine(tempDir, $"avatar_{Guid.NewGuid():N}.png");
                
                croppedBitmap.Save(tempFile);
                ResultPath = tempFile;
                
                Close();
            }
            catch
            {
                ResultPath = null;
                Close();
            }
        }

        public static async Task<string?> ShowCropWindowAsync(Window owner, string imagePath)
        {
            if (!File.Exists(imagePath)) return null;

            try
            {
                using var stream = File.OpenRead(imagePath);
                var bitmap = new Bitmap(stream);

                var tcs = new TaskCompletionSource<string?>();

                Dispatcher.UIThread.Post(() =>
                {
                    var window = new AvatarCropWindow
                    {
                        SourceImage = bitmap
                    };

                    window.Closed += (s, e) =>
                    {
                        tcs.TrySetResult(window.ResultPath);
                        bitmap.Dispose();
                    };

                    window.ShowDialog(owner);
                });

                return await tcs.Task;
            }
            catch
            {
                return null;
            }
        }
    }
}

