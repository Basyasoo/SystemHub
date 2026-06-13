using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using MacStyleHub.ViewModels;

namespace MacStyleHub.Views
{
    public partial class MediaPlaybackView : UserControl
    {
        private DispatcherTimer? _visualizerTimer;
        private double _visualizerPhase = 0;
        private readonly Random _rand = new();

        public MediaPlaybackView()
        {
            InitializeComponent();
        }

        private void OnSessionCardPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.DataContext is MediaSessionInfo sessionInfo)
            {
                if (DataContext is MediaPlaybackViewModel vm)
                {
                    vm.SelectSession(sessionInfo.AppId);
                }
            }
        }

        protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            
            _visualizerTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(40) // ~25 FPS
            };
            _visualizerTimer.Tick += UpdateVisualizer;
            _visualizerTimer.Start();
        }

        protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _visualizerTimer?.Stop();
            _visualizerTimer = null;
        }

        private void UpdateVisualizer(object? sender, EventArgs e)
        {
            var canvas = this.Find<Canvas>("VisualizerCanvas");
            if (canvas == null || !canvas.IsVisible) return;

            // Get ToolsViewModel from parent window context
            var mainWin = (Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            var windowVM = mainWin?.DataContext as MainWindowViewModel;
            var vm = windowVM?.ToolsVM;
            if (vm == null) return;

            // Clear previous bars
            canvas.Children.Clear();

            double width = canvas.Bounds.Width;
            double height = canvas.Bounds.Height;
            if (width <= 0) width = 180;
            if (height <= 0) height = 110;

            int numBars = 16;
            double gap = 2.5;
            double barWidth = (width - (gap * (numBars - 1))) / numBars;
            if (barWidth <= 0) barWidth = 8;

            bool isMediaPlaying = DataContext is MediaPlaybackViewModel playbackVM && playbackVM.IsPlaying;
            bool isActive = vm.IsEqualizerEnabled && isMediaPlaying;

            _visualizerPhase += 0.15;

            for (int i = 0; i < numBars; i++)
            {
                double targetHeight = 4; // idle floor height
                
                if (vm.IsEqualizerEnabled)
                {
                    if (isActive)
                    {
                        double sinVal = Math.Sin(_visualizerPhase + i * 0.7);
                        double factor = 0.4 + 0.6 * _rand.NextDouble();
                        targetHeight = 10 + (sinVal + 1.0) * 0.5 * (height - 20) * factor;
                        
                        double eqMod = 1.0;
                        if (i < 2) eqMod = 1.0 + (vm.BassBoostLevel + vm.Eq60Hz) / 12.0;
                        else if (i < 4) eqMod = 1.0 + vm.Eq170Hz / 12.0;
                        else if (i < 6) eqMod = 1.0 + vm.Eq310Hz / 12.0;
                        else if (i < 8) eqMod = 1.0 + vm.Eq600Hz / 12.0;
                        else if (i < 10) eqMod = 1.0 + vm.Eq1kHz / 12.0;
                        else if (i < 12) eqMod = 1.0 + vm.Eq3kHz / 12.0;
                        else if (i < 13) eqMod = 1.0 + vm.Eq6kHz / 12.0;
                        else if (i < 14) eqMod = 1.0 + vm.Eq12kHz / 12.0;
                        else if (i < 15) eqMod = 1.0 + vm.Eq14kHz / 12.0;
                        else eqMod = 1.0 + vm.Eq16kHz / 12.0;

                        if (eqMod < 0.1) eqMod = 0.1;
                        targetHeight *= eqMod;
                    }
                    else
                    {
                        targetHeight = 6 + Math.Sin(_visualizerPhase * 0.5 + i * 0.4) * 3;
                    }
                }

                if (targetHeight < 3) targetHeight = 3;
                if (targetHeight > height) targetHeight = height;

                double totalWidth = numBars * barWidth + (numBars - 1) * gap;
                double startOffset = (width - totalWidth) / 2;
                if (startOffset < 0) startOffset = 0;

                double maxRadius = targetHeight / 2;
                double cornerRadius = Math.Min(barWidth / 2, maxRadius);

                var rect = new Avalonia.Controls.Shapes.Rectangle
                {
                    Width = barWidth,
                    Height = targetHeight,
                    Fill = new SolidColorBrush(Color.Parse("#0A84FF")), // AccentBlue
                    RadiusX = cornerRadius,
                    RadiusY = cornerRadius
                };

                Canvas.SetLeft(rect, startOffset + i * (barWidth + gap));
                Canvas.SetTop(rect, height - targetHeight);
                canvas.Children.Add(rect);
            }
        }
    }
}
