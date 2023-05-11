
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace GenerativeArt
{
    public partial class MainWindow : System.Windows.Window
    {
        // Random number generator to produce seeds for individual generators
        Random rnd = new();
        int _lastSeed = -1;

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
            int seed;
            if (cbxHoldSeed.IsChecked == true)
            {
                seed = _lastSeed;
            }
            else
            {
                seed = rnd.Next();
                _lastSeed = seed;
            }
            tbxSeed.Text = $"{seed}";
            Debug.Assert(_generators != null, nameof(_generators) + " != null");
            _generators[tabArtType.SelectedIndex].Generate(seed);
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
            // ReSharper disable ConvertToConstant.Local
            var messageBoxText = "Really initialize and lose settings?";
            var caption = "Initialization";
            var button = MessageBoxButton.YesNo;
            var icon = MessageBoxImage.Warning;
            // ReSharper restore ConvertToConstant.Local
            var result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
            if (result == MessageBoxResult.No) return;

            Debug.Assert(_generators != null, nameof(_generators) + " != null");
            _generators[tabArtType.SelectedIndex].Initialize();
            return;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Event handler for grid loaded. </summary>
        ///
        /// <remarks>   We're able to snag the size of the bitmap when the grid is
        ///             properly loaded so we do that here
        ///             Darrell Plank, 4/20/2023. </remarks>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Routed event information. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            ArtWidth = (int)ArtColumn.ActualWidth;
            ArtHeight = (int)ArtRow.ActualHeight;
            _generators = new List<IGenerator>()
            {
                new CrabNebula.CrabNebula(this),
                new NoiseGenerator.NoiseGenerator(this),
                new ShapesGenerator.Shapes(this),
                new FlowGenerator.FlowGenerator(this),
            };
            _generators.ForEach(g => g.Initialize());
            OnGenerate(this, new RoutedEventArgs());

            // Set up data contexts for the tab pages so that data binding works properly
            pgNebula.DataContext = _generators[0];
            pgNoise.DataContext = _generators[1];
            pgShapes.DataContext = _generators[2];
            pgFlow.DataContext = _generators[3];
        }
        private void OnSave(object sender, RoutedEventArgs e)
        {
            Debug.Assert(_generators != null, nameof(_generators) + " != null");
            if (!_generators[tabArtType.SelectedIndex].DoesSerialization)
            {
                // ReSharper disable ConvertToConstant.Local
                var messageBoxText = "This generator does not support saving.";
                var caption = "Save";
                var button = MessageBoxButton.OK;
                var icon = MessageBoxImage.Warning;
                // ReSharper restore ConvertToConstant.Local
                MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                return;
            }
            var json = _generators[tabArtType.SelectedIndex].Serialize(_lastSeed);
            // Create SaveFileDialog
            var ext = _generators[tabArtType.SelectedIndex].SerialExtension;

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = ext,
                Filter = $"Generator documents (.{ext})|*.{ext}"

            };

            // Launch SaveFileDialog by calling ShowDialog method
            var result = saveFileDialog.ShowDialog();
            if (result == true)
            {
                File.WriteAllText(saveFileDialog.FileName, json);
            }
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            Debug.Assert(_generators != null, nameof(_generators) + " != null");
            if (!_generators[tabArtType.SelectedIndex].DoesSerialization)
            {
                // ReSharper disable ConvertToConstant.Local
                var messageBoxText = "This generator does not support saving.";
                var caption = "Save";
                var button = MessageBoxButton.OK;
                var icon = MessageBoxImage.Warning;
                // ReSharper restore ConvertToConstant.Local
                MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                return;
            }

            var ext = _generators[tabArtType.SelectedIndex].SerialExtension;
            // Create OpenFileDialog
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                AddExtension = true,
                DefaultExt = ext,
                Filter = $"Generator documents (.{ext})|*.{ext}"
            };

            // Launch OpenFileDialog by calling ShowDialog method
            var result = openFileDialog.ShowDialog();
            if (result == true)
            {
                var json = File.ReadAllText(openFileDialog.FileName);
                _lastSeed = _generators[tabArtType.SelectedIndex].Deserialize(json);
                tbxSeed.Text = $"{_lastSeed}";
                _generators[tabArtType.SelectedIndex].Generate(_lastSeed);
            }
        }
    }
}
