using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

using System.ComponentModel.Composition;

namespace ReadOnlyEditorToggle
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class ReadOnlyAdornmentTextViewCreationListener
        : IWpfTextViewCreationListener
    {
        [Import]
        internal IVsEditorAdaptersFactoryService Adapter = null;

        public void TextViewCreated(IWpfTextView textView)
        {
            _ = textView.Properties.GetOrCreateSingletonProperty(
                () => new ReadOnlyAdornmentController(textView, Adapter));
        }
    }
}