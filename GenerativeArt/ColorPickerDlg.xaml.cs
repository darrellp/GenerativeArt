using System.Windows;
using System.Windows.Media;

namespace GenerativeArt
{
    /// <summary>
    /// Interaction logic for ColorPickerDlg.xaml
    /// </summary>
    public partial class ColorPickerDlg : Window
    {
        public ColorPickerDlg()
        {
            InitializeComponent();
        }

        public static (bool? IsAccepted, Color color) GetUserColor(Color initialColor)
        {
            var dlg = new ColorPickerDlg();
            dlg.cp.SelectedColor = initialColor;
            var isAccepted = dlg.ShowDialog();
            return (isAccepted, dlg.cp.SelectedColor);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
