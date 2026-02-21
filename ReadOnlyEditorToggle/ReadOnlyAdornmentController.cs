using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

using System;
namespace ReadOnlyEditorToggle
{

    public sealed class ReadOnlyAdornmentController
    {
        private readonly IWpfTextView _view;
        private readonly ReadOnlyAdornmentVisuals _visuals;

        private bool _lastReadOnly;
        private double _lastWidth;
        private double _lastHeight;

        public ReadOnlyAdornmentController(IWpfTextView view)
        {
            _view = view;
            _visuals = new ReadOnlyAdornmentVisuals(view);

            _lastReadOnly = view.TextBuffer.IsReadOnly(0);
            _lastWidth = view.ViewportWidth;
            _lastHeight = view.ViewportHeight;

            // Events
            view.TextBuffer.ReadOnlyRegionsChanged += OnReadOnlyChanged;
            view.ViewportWidthChanged += OnViewportChanged;
            view.ViewportHeightChanged += OnViewportChanged;
            view.LayoutChanged += OnLayoutChanged;
            view.Closed += OnClosed;

            Update();
        }

        private void OnReadOnlyChanged(object sender, SnapshotSpanEventArgs e)
        {
            Update();
        }

        private void OnViewportChanged(object sender, EventArgs e)
        {
            Update();
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            Update();
        }

        private void Update()
        {
            bool isReadOnly = _view.TextBuffer.IsReadOnly(0);
            double width = _view.ViewportWidth;
            double height = _view.ViewportHeight;

            bool stateChanged = isReadOnly != _lastReadOnly;
            bool sizeChanged = width != _lastWidth || height != _lastHeight;

            if (!stateChanged && !sizeChanged)
                return;

            _lastReadOnly = isReadOnly;
            _lastWidth = width;
            _lastHeight = height;

            _visuals.Update(isReadOnly, width, height);
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _view.TextBuffer.ReadOnlyRegionsChanged -= OnReadOnlyChanged;
            _view.ViewportWidthChanged -= OnViewportChanged;
            _view.ViewportHeightChanged -= OnViewportChanged;
            _view.LayoutChanged -= OnLayoutChanged;
            _view.Closed -= OnClosed;
        }
    }
}