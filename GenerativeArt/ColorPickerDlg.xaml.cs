using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
