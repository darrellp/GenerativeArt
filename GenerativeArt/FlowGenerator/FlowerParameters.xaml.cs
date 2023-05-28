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

namespace GenerativeArt.FlowGenerator
{
    /// <summary>
    /// Interaction logic for FlowerParameters.xaml
    /// </summary>
    public partial class FlowerParameters : Window
    {
        public FlowerParameters()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            foreach (var be in BindingOperations.GetSourceUpdatingBindings(this))
            {
                be.UpdateSource();
            }
        }

        private void sldrCtrRadius_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lblCtrRadius.Content = $"Center Radius: {e.NewValue:##0}";
        }

        private void sldrPetalLength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lblPetalLength.Content = $"Petal Length: {e.NewValue:##0}";
        }

        private void sldrDropOff_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lblDropOff.Content = $"Dropoff: {e.NewValue:##0}";
        }
    }
}
