using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GenerativeArt.ShapesGenerator
{
    class Shapes : IGenerator
    {
        #region Private Variables
        private MainWindow _ourWindow { get; }
        private Parameters _params { get; set; } = new Parameters();
        private int ArtWidth => _ourWindow.ArtWidth;
        private int ArtHeight => _ourWindow.ArtHeight;
        #endregion

        #region Constructor
        internal Shapes(MainWindow ourWindow)
        {
            _ourWindow = ourWindow;
            HookParameterControls();
        }
        #endregion

        #region IGenerator interface
        public void Generate()
        {
            GatherParameters();
            var wbmp = BitmapFactory.New(ArtWidth, ArtHeight);
            using (wbmp.GetBitmapContext())
            {
                wbmp.Clear(Colors.Black);
                var cellSize = (double)ArtWidth / _params.GridCount;
                var baseRadius = cellSize * _params.BaseScale / 2;
                for (int ix = 0; ix < _params.GridCount; ix++)
                {
                    for (int iy = 0; iy < _params.GridCount; iy++)
                    {
                        var xc = (ix + 0.5) * cellSize;
                        var yc = (iy + 0.5) * cellSize;
                        wbmp.FillEllipseCentered((int)xc, (int)yc, (int)baseRadius, (int)baseRadius, Colors.Red);
                    }
                }
                _ourWindow.Art.Source = wbmp;
            }
        }

        public void Initialize()
        {
            _params = new Parameters();
            DistributeParameters();
        }

        public void Kill()
        {
        }
        #endregion

        #region Parameter handling
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Distribute parameters to the controls on the tabs page. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void DistributeParameters()
        {
            _ourWindow.sldrShGridCount.Value = _params.GridCount;
            _ourWindow.sldrShBaseScale.Value = _params.BaseScale;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Gather parameters from the tabs page. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void GatherParameters()
        {
            _params.GridCount = (int)_ourWindow.sldrShGridCount.Value;
            _params.BaseScale = _ourWindow.sldrShBaseScale.Value;

        }

        #region Hooks
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Hook tab page controls. </summary>
        ///
        /// <remarks>   Darrell Plank, 4/20/2023. </remarks>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        private void HookParameterControls()
        {
            _ourWindow.sldrShGridCount.ValueChanged += sldrShGridCount_ValueChanged;
            _ourWindow.sldrShBaseScale.ValueChanged += SldrsldrShBaseScale_ValueChanged;
        }

        private void SldrsldrShBaseScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblShBaseScale.Content = $"Base Scale: {e.NewValue:#0.00}";
        }

        private void sldrShGridCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _ourWindow.lblShGridCount.Content = $"Grid Count: {e.NewValue}";
        }
        #endregion
        #endregion

    }
}
