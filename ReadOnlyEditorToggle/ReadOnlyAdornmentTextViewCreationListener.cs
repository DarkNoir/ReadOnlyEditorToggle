using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

using System.ComponentModel.Composition;

namespace ReadOnlyEditorToggle
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("any")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal sealed class ReadOnlyAdornmentTextViewCreationListener
        : IWpfTextViewCreationListener
    {
        public void TextViewCreated(IWpfTextView textView)
        {
            _ = new ReadOnlyAdornmentController(textView);
        }
    }

}
