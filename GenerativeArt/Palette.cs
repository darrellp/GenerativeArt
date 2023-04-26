using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Media;

namespace GenerativeArt
{
    internal class Palette
    {
        #region Private variables
        #region Enables
        public bool[] ColorEnabled = new bool[4];
        private bool _enabled1;
        public bool Enabled1
        {
            get => _enabled1;
            set => SetField(ref _enabled1, value);
        }
        private bool _enabled2;
        public bool Enabled2
        {
            get => _enabled2;
            set => SetField(ref _enabled2, value);
        }
        private bool _enabled3;
        public bool Enabled3
        {
            get => _enabled3;
            set => SetField(ref _enabled3, value);
        }
        private bool _enabled4;
        public bool Enabled4
        {
            get => _enabled4;
            set => SetField(ref _enabled4, value);
        }
        #endregion

        #region Colors
        public HSB[] ColorsHSB = new HSB[4];
        private Color _color1;
        public Color Color1
        {
            get => _color1;
            set => SetField(ref _color1, value);
        }
        private Color _color2;
        public Color Color2
        {
            get => _color2;
            set => SetField(ref _color2, value);
        }
        private Color _color3;
        public Color Color3
        {
            get => _color3;
            set => SetField(ref _color3, value);
        }
        private Color _color4;
        public Color Color4
        {
            get => _color4;
            set => SetField(ref _color4, value);
        }
        #endregion

        #region Variation
        private double _varH;

        public double VarH
        {
            get => _varH;
            set => SetField(ref _varH, value);
        }

        private double _varS;
        public double VarS
        {
            get => _varS;
            set => SetField(ref _varS, value);
        }

        private double _varB;
        public double VarB
        {
            get => _varB;
            set => SetField(ref _varB, value);
        }
        #endregion
        #endregion

        #region Constructor
        internal Palette(Palette palette)
        {
            VarH = palette.VarH;
            VarS = palette.VarS;
            VarB = palette.VarB;
            Color1 = palette.Color1;
            Color2 = palette.Color2;
            Color3 = palette.Color3;
            Color4 = palette.Color4;
            Enabled1 = palette.Enabled1;
            Enabled2 = palette.Enabled2;
            Enabled3 = palette.Enabled3;
            Enabled4 = palette.Enabled4;
            PaletteRGBToHSB();
        }

        internal Palette() {}

        private void PaletteRGBToHSB()
        {
            var arrColor = new[] { Color1, Color2, Color3, Color4 };
            ColorsHSB = arrColor.Select(c => new HSB(c)).ToArray();
            ColorEnabled = new bool[] { Enabled1, Enabled2, Enabled3, Enabled4 };

        }
        #endregion

        #region Property changes
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion

        #region Dialog
        internal Palette GetUserPalette()
        {
            var palette = new Palette(this);
            return palette.RunDialog() ? palette : this;
        }

        internal bool RunDialog()
        {

            var dlg = new PaletteDlg
            {
                DataContext = this,
            };
            dlg.btnColor1.Background = new SolidColorBrush(Color1);
            dlg.btnColor2.Background = new SolidColorBrush(Color2);
            dlg.btnColor3.Background = new SolidColorBrush(Color3);
            dlg.btnColor4.Background = new SolidColorBrush(Color4);
            dlg.btnColor1.Click +=ChangeColor;
            dlg.btnColor2.Click +=ChangeColor;
            dlg.btnColor3.Click +=ChangeColor;
            dlg.btnColor4.Click +=ChangeColor;

            var result = dlg.ShowDialog() ?? false;
            if (result)
            {
                PaletteRGBToHSB();
                return true;
            }

            return false;
        }

        private void ChangeColor(object sender, System.Windows.RoutedEventArgs e)
        {
            var btn = sender as Button;
            Debug.Assert(btn != null);
            var index = btn.Name[^1] - '1';
            
            var brush = (SolidColorBrush)btn.Background;
            var (isAccepted, newColor) = ColorPickerDlg.GetUserColor(brush.Color);
            if (isAccepted == true)
            {
                btn.Background = new SolidColorBrush(newColor);
                switch (index)
                {
                    case 0: 
                        Color1 = newColor;
                        break;
                    case 1:
                        Color2 = newColor;
                        break;
                    case 2:
                        Color3 = newColor;
                        break;
                    default:
                        Color4 = newColor;
                        break;
                }
                PaletteRGBToHSB();
            }
        }

        internal Color SelectColor(Random rnd)
        {
            var selected = new List<HSB>(4);

            for (var i = 0; i < 4; i++)
            {
                if (ColorEnabled[i])
                {
                    selected.Add(ColorsHSB[i]);
                }
            }

            var hsbSelected = selected[rnd.Next(selected.Count)];
            var h = hsbSelected.H + 2*(rnd.NextDouble() - 0.5) * VarH;
            if (h < 0) h += 360;
            else if (h > 360) h -= 360;
            var s = hsbSelected.S  + 2*(rnd.NextDouble() - 0.5) * VarS;
            if (s < 0) s = 0;
            if (s > 1) s = 1;
            var b = hsbSelected.B  + 2*(rnd.NextDouble() - 0.5) * VarB;
            if (b < 0) b = 0;
            if (b > 1) b = 1;
            return HSB.ColorFromHSB(h, s, b);
        }
        #endregion
    }
}
