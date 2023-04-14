using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
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

        public MainWindow()
        {
            InitializeComponent();
        }

        // Current code is based on blog post here:
        //      https://generateme.wordpress.com/2018/10/24/smooth-rendering-log-density-mapping/
        // with a few differences - primarily that it's written in C# on a WriteableBitmap rather than in Processing.
        // I've changed some of the parameters to suit me and though I tried some public domain noise producers, none
        // seemed like what I wanted or I couldn't figure them out so wrote my own Perlin noise generator.  Colors are
        // done in radial bands rather than whatever he does (which I think was a horizontal single gradation.  Probably
        // other stuff that I didn't really understand in his code and just wrote the way that seemed right to me.
        // Eventually, I will probably do other generative stuff and OnGenerate will generate one of s set of different
        // algorithms here but for now this is the only one.  This stuff should definitely be put into a class of it's
        // own but pressing forward with this until I start on a second algorithm.

        private void OnGenerate(object sender, RoutedEventArgs e)
        {
            Debug.Assert(_wbmp != null, nameof(_wbmp) + " != null");
            _wbmp.Clear(Colors.Black);
            var crabNebula = new CrabNebula.CrabNebula(_wbmp);
            crabNebula.Generate();
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
            OnGenerate(this, new RoutedEventArgs());
        }
    }
}
