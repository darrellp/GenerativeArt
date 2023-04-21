
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace GenerativeArt
{
    public partial class MainWindow : System.Windows.Window
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the width of the art. </summary>
        ///
        /// <value> The width of the art. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public int ArtWidth { get; private set; }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gets or sets the height of the art. </summary>
        ///
        /// <value> The height of the art. </value>
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        public int ArtHeight { get; private set; }
        /// <summary>   The generators. </summary>
        private List<IGenerator>? _generators;

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Default constructor. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public MainWindow()
        {
            InitializeComponent();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Handler for the Generate button. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information to send to registered event handlers. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void OnGenerate(object sender, RoutedEventArgs e)
        {
            Debug.Assert(_generators != null, nameof(_generators) + " != null");
            _generators[tabArtType.SelectedIndex].Generate();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Event handler. Called by BtnInitialize for click events. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Routed event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void BtnInitialize_Click(object sender, RoutedEventArgs e)
        {
            Debug.Assert(_generators != null, nameof(_generators) + " != null");
            _generators[tabArtType.SelectedIndex].Initialize();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Event handler. Called by Rectangle for loaded events.
        ///              </summary>
        ///
        /// <remarks>   I'm embarrassed that I'm misusing this load on a dummy rectangle to 
        ///             retrieve sizes but it seems that my art image claims it's size 0 until it
        ///             gets drawn to which means I need something else to be given the actual
        ///             size of our space.  Still - there's gotta be a better way.
        ///             Darrell Plank, 4/20/2023. </remarks>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Routed event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Rectangle_Loaded(object sender, RoutedEventArgs e)
        {
            // There's got to be a better way than this but I'm using this to determine when the
            // size of the WritableBitmap is actually known.  If I check when the Image control is loaded
            // then I get zero size in spite of the fact that art is supposed to fill the entire
            // cell. If I make a dummy rectangle though and check when it's loaded I get the
            // proper size.  I'm not sure what the proper way to do this is, but it can't be this!

            ArtWidth = (int)RctSize.ActualWidth;
            ArtHeight = (int)RctSize.ActualHeight;

            // Generators know about/manage their individual tab pages and so must do the I/O on those tabs.
            // They also do the actual drawing on the art image.  For these reasons they need access
            // to the main window which is why it's passed down as a parameter to their constructors.
            _generators = new List<IGenerator>()
            {
                new CrabNebula.CrabNebula(this),
                new NoiseGenerator.NoiseGenerator(this),
            };
            _generators.ForEach(g => g.Initialize());
            OnGenerate(this, new RoutedEventArgs());
        }
    }
}
