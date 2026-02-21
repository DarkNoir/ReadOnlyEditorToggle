using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

using System;

namespace ReadOnlyEditorToggle
{
    public sealed class ReadOnlyAdornmentController
    {
        private readonly IWpfTextView _view;
        private readonly IVsEditorAdaptersFactoryService _adapter;
        private readonly ITextBuffer _buffer;
        private readonly ReadOnlyAdornmentVisuals _visuals;

        private IReadOnlyRegion _region;

        private bool _lastReadOnly;
        private double _lastWidth;
        private double _lastHeight;

        public bool IsLocked { get; private set; }

        public ReadOnlyAdornmentController(IWpfTextView view, IVsEditorAdaptersFactoryService adapter)
        {
            _view = view;
            _adapter = adapter;

            // Use the view's buffer directly
            _buffer = view.TextBuffer;

            _visuals = new ReadOnlyAdornmentVisuals(view);

            _lastReadOnly = _buffer.IsReadOnly(0);
            _lastWidth = view.ViewportWidth;
            _lastHeight = view.ViewportHeight;

            _buffer.ReadOnlyRegionsChanged += OnReadOnlyChanged;
            _view.ViewportWidthChanged += OnViewportChanged;
            _view.ViewportHeightChanged += OnViewportChanged;
            _view.LayoutChanged += OnLayoutChanged;
            _view.Closed += OnClosed;

            Update();
        }

        public void Toggle()
        {
            if (IsLocked)
                Unlock();
            else
                Lock();
        }

        private void Lock()
        {
            if (IsLocked)
                return;

            using (IReadOnlyRegionEdit edit = _buffer.CreateReadOnlyRegionEdit())
            {
                ITextSnapshot snapshot = _buffer.CurrentSnapshot;
                _region = edit.CreateReadOnlyRegion(new Span(0, snapshot.Length));
                edit.Apply();
            }

            // Try to set the VS tab lock icon if this buffer has an IVsTextBuffer
            if (_adapter.GetBufferAdapter(_view.TextBuffer) is IVsTextBuffer vsBuffer)
            {
                vsBuffer.GetStateFlags(out uint flags);
                flags |= (uint)BUFFERSTATEFLAGS.BSF_USER_READONLY;
                vsBuffer.SetStateFlags(flags);
            }

            IsLocked = true;
            Update();
        }

        private void Unlock()
        {
            if (!IsLocked)
                return;

            using (IReadOnlyRegionEdit edit = _buffer.CreateReadOnlyRegionEdit())
            {
                if (_region != null)
                    edit.RemoveReadOnlyRegion(_region);

                edit.Apply();
            }

            if (_adapter.GetBufferAdapter(_view.TextBuffer) is IVsTextBuffer vsBuffer)
            {
                vsBuffer.GetStateFlags(out uint flags);
                flags &= ~(uint)BUFFERSTATEFLAGS.BSF_USER_READONLY;
                vsBuffer.SetStateFlags(flags);
            }

            _region = null;
            IsLocked = false;

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
            bool isReadOnly = _buffer.IsReadOnly(0);
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
            _buffer.ReadOnlyRegionsChanged -= OnReadOnlyChanged;
            _view.ViewportWidthChanged -= OnViewportChanged;
            _view.ViewportHeightChanged -= OnViewportHeightChanged;
            _view.LayoutChanged -= OnLayoutChanged;
            _view.Closed -= OnClosed;
        }

        private void OnViewportHeightChanged(object sender, EventArgs e)
        {
            Update();
        }
    }
}