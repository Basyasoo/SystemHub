using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace SystemHub.Views
{
    public partial class OcrOverlayWindow : Window
    {
        private Point _startPoint;
        private bool _isDragging;
        
        private Canvas? _mainCanvas;
        private Border? _selectionBorder;
        private Avalonia.Controls.Shapes.Path? _dimPath;

        public Rect? SelectedRect { get; private set; }

        public OcrOverlayWindow()
        {
            InitializeComponent();
            _mainCanvas = this.FindControl<Canvas>("MainCanvas");
            _selectionBorder = this.FindControl<Border>("SelectionBorder");
            _dimPath = this.FindControl<Avalonia.Controls.Shapes.Path>("DimPath");
        }

        public OcrOverlayWindow(Bitmap screenShotImage) : this()
        {
            var screenImage = this.FindControl<Image>("ScreenImage");
            if (screenImage != null)
            {
                screenImage.Source = screenShotImage;
            }

            // Lock window dimensions to screenshot size
            Width = screenShotImage.Size.Width;
            Height = screenShotImage.Size.Height;
            
            // Set initial overlay backdrop to cover whole screen
            if (_dimPath != null)
            {
                _dimPath.Data = new RectangleGeometry(new Rect(0, 0, Width, Height));
            }
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            Focus();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape)
            {
                SelectedRect = null;
                Close();
            }
        }

        private void Canvas_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_mainCanvas == null) return;
            
            var pointerProps = e.GetCurrentPoint(_mainCanvas).Properties;
            if (pointerProps.IsLeftButtonPressed)
            {
                _startPoint = e.GetPosition(_mainCanvas);
                _isDragging = true;
                
                if (_selectionBorder != null)
                {
                    _selectionBorder.IsVisible = true;
                    Canvas.SetLeft(_selectionBorder, _startPoint.X);
                    Canvas.SetTop(_selectionBorder, _startPoint.Y);
                    _selectionBorder.Width = 0;
                    _selectionBorder.Height = 0;
                }
                e.Handled = true;
            }
        }

        private void Canvas_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (_mainCanvas == null) return;

            if (_isDragging)
            {
                var currentPoint = e.GetPosition(_mainCanvas);
                double x = Math.Min(_startPoint.X, currentPoint.X);
                double y = Math.Min(_startPoint.Y, currentPoint.Y);
                double w = Math.Abs(_startPoint.X - currentPoint.X);
                double h = Math.Abs(_startPoint.Y - currentPoint.Y);

                if (_selectionBorder != null)
                {
                    Canvas.SetLeft(_selectionBorder, x);
                    Canvas.SetTop(_selectionBorder, y);
                    _selectionBorder.Width = w;
                    _selectionBorder.Height = h;
                }

                // Update dim path geometry (entire screen MINUS selected rectangle)
                if (_dimPath != null)
                {
                    var screenRect = new Rect(0, 0, Width, Height);
                    var selectRect = new Rect(x, y, w, h);
                    var combinedGeometry = new CombinedGeometry(
                        GeometryCombineMode.Exclude,
                        new RectangleGeometry(screenRect),
                        new RectangleGeometry(selectRect));
                    _dimPath.Data = combinedGeometry;
                }
                e.Handled = true;
            }
        }

        private void Canvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                if (_mainCanvas == null) return;

                var currentPoint = e.GetPosition(_mainCanvas);
                double x = Math.Min(_startPoint.X, currentPoint.X);
                double y = Math.Min(_startPoint.Y, currentPoint.Y);
                double w = Math.Abs(_startPoint.X - currentPoint.X);
                double h = Math.Abs(_startPoint.Y - currentPoint.Y);

                // Consider it a valid selection if width/height are reasonable
                if (w > 5 && h > 5)
                {
                    SelectedRect = new Rect(x, y, w, h);
                }
                else
                {
                    SelectedRect = null;
                }
                
                Close();
                e.Handled = true;
            }
        }
    }
}

