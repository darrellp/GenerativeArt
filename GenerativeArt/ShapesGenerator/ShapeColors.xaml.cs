using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GenerativeArt.ShapesGenerator
{
    /// <summary>
    /// Interaction logic for ShapeColors.xaml
    /// </summary>
    public partial class ShapeColors : Window
    {
        private Color[] circleColors = new Color[4];
        private Color[] squareColors = new Color[4];
        public ShapeColors()
        {
            InitializeComponent();
        }

        private void Okay_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void HandleColorButton(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            Debug.Assert(btn != null);
            var brush = (SolidColorBrush)btn.Background;
            var (isAccepted, newColor) = ColorPickerDlg.GetUserColor(brush.Color);
            if (isAccepted == true)
            {
                btn.Background = new SolidColorBrush(newColor);
            }
        }

        private void chkCircleColor1_Click(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            Debug.Assert(cb != null);
            var isChecked = cb.IsChecked;
            var name = cb.Name;
            var indexChar = name[^1];
            var index = indexChar - '1';
            var shapeName = name[3] == 'C' ? "Circle" : "Square";
            var grid = cb.Parent as Grid;
            Debug.Assert(grid != null);
            var btnName = $"btn{shapeName}Color{indexChar}";
            var btn = grid.FindName(btnName) as Button;
            Debug.Assert(btn != null);
            btn.IsEnabled = cb.IsChecked == true;
        }
    }
}
