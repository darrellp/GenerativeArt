using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GenerativeArt
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private int _width, _height;
        private WriteableBitmap? _wbmp;
        private List<IGenerator>? _generators;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnGenerate(object sender, RoutedEventArgs e)
        {
            Debug.Assert(_wbmp != null, nameof(_wbmp) + " != null");
            Debug.Assert(_generators != null, nameof(_generators) + " != null");
            _wbmp.Clear(Colors.Black);
            _generators[tabArtType.SelectedIndex].Generate();
        }

        private void Rectangle_Loaded(object sender, RoutedEventArgs e)
        {
            // There's got to be a better way than this but I'm using this to determine when the
            // size of the WritableBitmap is actually known.  If I check when the Image control is loaded
            // then I get zero size in spite of the fact that art is supposed to fill the entire
            // cell. If I make a dummy rectangle though and check when it's loaded I get the
            // proper size.  I'm not sure what the proper way to do this is, but it can't be this!

            _width = (int)RctSize.ActualWidth;
            _height = (int)RctSize.ActualHeight;
            _wbmp = BitmapFactory.New(_width, _height);
            Art.Source = _wbmp;
            _generators = new List<IGenerator>()
            {
                new CrabNebula.CrabNebula(_wbmp),
                new ClearArt(_wbmp),
            };
            _generators.ForEach(g => g.Initialize());
            OnGenerate(this, new RoutedEventArgs());
        }
    }

    // Dummy Class to test tabs stuff
    class ClearArt : IGenerator
    {
        private readonly WriteableBitmap _wbmp;

        public void Generate()
        {
            _wbmp.Clear(Colors.Black);
        }

        public void Initialize()
        {
        }

        public ClearArt(WriteableBitmap wbmp)
        {
            _wbmp = wbmp;
        }
    }
}
