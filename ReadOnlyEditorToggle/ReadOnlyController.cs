using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace ReadOnlyEditorToggle
{
    public class ReadOnlyController
    {
        private readonly ITextBuffer _Buffer;
        private readonly IVsEditorAdaptersFactoryService _Adapter;
        private ITextSnapshot _Snapshot;
        private IReadOnlyRegion _Region;

        private ReadOnlyController(ITextBuffer buffer, IVsEditorAdaptersFactoryService adapter)
        {
            _Buffer = buffer;
            _Adapter = adapter;
        }
        public bool IsLocked { get; private set; }

        public static ReadOnlyController GetOrCreate(ITextBuffer buffer, IVsEditorAdaptersFactoryService adapter)
        {
            if (!buffer.Properties.TryGetProperty(typeof(ReadOnlyController), out ReadOnlyController controller))
            {
                controller = new ReadOnlyController(buffer, adapter);
                buffer.Properties.AddProperty(typeof(ReadOnlyController), controller);
            }

            return controller;
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

            using (IReadOnlyRegionEdit edit = _Buffer.CreateReadOnlyRegionEdit())
            {
                _Snapshot = _Buffer.CurrentSnapshot;
                _Region = edit.CreateReadOnlyRegion(new Span(0, _Snapshot.Length));
                _ = edit.Apply();
            }

            // Set tab lock icon
            if (_Adapter.GetBufferAdapter(_Buffer) is IVsTextBuffer vsBuffer)
                _ = vsBuffer.SetStateFlags((uint)BUFFERSTATEFLAGS.BSF_USER_READONLY);
            IsLocked = true;
        }

        private void Unlock()
        {
            if (!IsLocked)
                return;

            using (IReadOnlyRegionEdit edit = _Buffer.CreateReadOnlyRegionEdit())
            {
                edit.RemoveReadOnlyRegion(_Region);
                _ = edit.Apply();
            }

            // Remove tab lock icon
            if (_Adapter.GetBufferAdapter(_Buffer) is IVsTextBuffer vsBuffer)
                _ = vsBuffer.SetStateFlags(0);

            _Region = null;
            _Snapshot = null;
            IsLocked = false;
        }
    }
}

