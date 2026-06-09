using Avalonia;
using Avalonia.Controls;

namespace MacStyleHub.Views
{
    public partial class WeatherIcon : UserControl
    {
        public static readonly StyledProperty<string> IconProperty =
            AvaloniaProperty.Register<WeatherIcon, string>(nameof(Icon), "sun");

        public string Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static readonly StyledProperty<double> StrokeThicknessProperty =
            AvaloniaProperty.Register<WeatherIcon, double>(nameof(StrokeThickness), 2.0);

        public double StrokeThickness
        {
            get => GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        public WeatherIcon()
        {
            InitializeComponent();
        }
    }
}
