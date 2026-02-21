using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

using System;

namespace ReadOnlyEditorToggle
{
    public sealed class ReadOnlyController
    {
        private readonly ITextBuffer _subjectBuffer;
        private readonly IVsEditorAdaptersFactoryService _adapter;
        private readonly ReadOnlyAdornmentVisuals _visuals;
        private readonly IWpfTextView _view;

        private bool _lastReadOnly;
        private double _lastWidth;
        private double _lastHeight;
        private IReadOnlyRegion _Region;
        private ITextSnapshot _Snapshot;

        public bool IsLocked { get; private set; }

        public ReadOnlyController(IWpfTextView view, IVsEditorAdaptersFactoryService adapter)
        {
            _view = view;
            _adapter = adapter;

            // Always operate on the subject (document) buffer
            _subjectBuffer = adapter.GetDocumentBuffer((IVsTextBuffer)view.TextBuffer);

            _visuals = new ReadOnlyAdornmentVisuals(view);

            _lastReadOnly = _subjectBuffer.IsReadOnly(0);
            _lastWidth = view.ViewportWidth;
            _lastHeight = view.ViewportHeight;

            view.TextBuffer.ReadOnlyRegionsChanged += OnReadOnlyChanged;
            view.ViewportWidthChanged += OnViewportChanged;
            view.ViewportHeightChanged += OnViewportChanged;
            view.LayoutChanged += OnLayoutChanged;
            view.Closed += OnClosed;

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

            using (IReadOnlyRegionEdit edit = _subjectBuffer.CreateReadOnlyRegionEdit())
            {
                ITextSnapshot snapshot = _subjectBuffer.CurrentSnapshot;
                edit.CreateReadOnlyRegion(new Span(0, snapshot.Length));
                edit.Apply();
            }

            if (_adapter.GetBufferAdapter(_subjectBuffer) is IVsTextBuffer vsBuffer)
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

            using (IReadOnlyRegionEdit edit = _subjectBuffer.CreateReadOnlyRegionEdit())
            {
                if (_Region != null)
                    edit.RemoveReadOnlyRegion(_Region);

                edit.Apply();
            }

            if (_adapter.GetBufferAdapter(_subjectBuffer) is IVsTextBuffer vsBuffer)
            {
                vsBuffer.GetStateFlags(out uint flags);
                flags &= ~(uint)BUFFERSTATEFLAGS.BSF_USER_READONLY;
                vsBuffer.SetStateFlags(flags);
            }

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
            bool isReadOnly = _subjectBuffer.IsReadOnly(0);
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

