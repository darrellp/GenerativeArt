using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GenerativeArt
{
    /// <summary>
    /// Interaction logic for PaletteDlg.xaml
    /// </summary>
    public partial class PaletteDlg : Window
    {
        public PaletteDlg()
        {
            InitializeComponent();
        }

        private void btnOkay_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
