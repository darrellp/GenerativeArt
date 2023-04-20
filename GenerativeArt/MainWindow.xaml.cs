using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace GenerativeArt
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public int ArtWidth { get; private set; }
        public int ArtHeight { get; private set; }
        private List<IGenerator>? _generators;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnGenerate(object sender, RoutedEventArgs e)
        {
            Debug.Assert(_generators != null, nameof(_generators) + " != null");
            _generators[tabArtType.SelectedIndex].Generate();
        }

        private void BtnInitialize_Click(object sender, RoutedEventArgs e)
        {
            Debug.Assert(_generators != null, nameof(_generators) + " != null");
            _generators[tabArtType.SelectedIndex].Initialize(this);
        }

        private void Rectangle_Loaded(object sender, RoutedEventArgs e)
        {
            // There's got to be a better way than this but I'm using this to determine when the
            // size of the WritableBitmap is actually known.  If I check when the Image control is loaded
            // then I get zero size in spite of the fact that art is supposed to fill the entire
            // cell. If I make a dummy rectangle though and check when it's loaded I get the
            // proper size.  I'm not sure what the proper way to do this is, but it can't be this!

            ArtWidth = (int)RctSize.ActualWidth;
            ArtHeight = (int)RctSize.ActualHeight;
            _generators = new List<IGenerator>()
            {
                new CrabNebula.CrabNebula(),
                new NoiseGenerator.NoiseGenerator(),
            };
            _generators.ForEach(g => g.Initialize(this));
            OnGenerate(this, new RoutedEventArgs());
        }
    }
}
