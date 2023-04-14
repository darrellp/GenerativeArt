using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MathNet.Numerics.Distributions;
using GenerativeArt.CrabNebula;
using static GenerativeArt.Utilities;

namespace GenerativeArt
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private int _width, _height;
        private WriteableBitmap? _wbmp;
        private List<IGenerator> _generators;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnGenerate(object sender, RoutedEventArgs e)
        {
            Debug.Assert(_wbmp != null, nameof(_wbmp) + " != null");
            _wbmp.Clear(Colors.Black);
            _generators[0].Generate();
        }

        private void Rectangle_Loaded(object sender, RoutedEventArgs e)
        {
            // There's got to be an easier way than this but I'm using this to determine when the
            // size of the WritableBitmap is actually known.  I'm not sure what the proper way to
            // do this is, but it can't be this!

            _width = (int)RctSize.ActualWidth;
            _height = (int)RctSize.ActualHeight;
            _wbmp = BitmapFactory.New(_width, _height);
            Art.Source = _wbmp;
            _generators = new List<IGenerator>()
            {
                new CrabNebula.CrabNebula(_wbmp),
            };
            OnGenerate(this, new RoutedEventArgs());
        }
    }
}
