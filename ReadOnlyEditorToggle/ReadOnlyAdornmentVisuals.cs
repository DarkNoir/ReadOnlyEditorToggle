using Microsoft.VisualStudio.Text.Editor;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ReadOnlyEditorToggle
{
    public sealed class ReadOnlyAdornmentVisuals
    {
        private readonly IAdornmentLayer _layer;
        private readonly Grid _container;
        private readonly Border _border;
        private readonly Rectangle _tint;

        private bool _isVisible;

        public ReadOnlyAdornmentVisuals(IWpfTextView view)
        {
            _layer = view.GetAdornmentLayer("ReadOnlyAdornmentLayer");

            _border = new Border
            {
                BorderBrush = Brushes.Red,
                BorderThickness = new Thickness(2),
                IsHitTestVisible = false
            };

            _tint = new Rectangle
            {
                Fill = new SolidColorBrush(Color.FromArgb(40, 255, 0, 0)),
                IsHitTestVisible = false
            };

            _container = new Grid();
            _container.Children.Add(_tint);
            _container.Children.Add(_border);

            _layer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, null, _container, null);
        }

        public void Update(bool visible, double width, double height)
        {
            if (visible != _isVisible)
            {
                _container.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                _isVisible = visible;
            }

            _container.Width = width;
            _container.Height = height;
        }
    }
}
