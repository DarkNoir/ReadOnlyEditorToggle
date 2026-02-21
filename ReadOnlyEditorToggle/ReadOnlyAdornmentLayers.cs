using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

using System.ComponentModel.Composition;

namespace ReadOnlyEditorToggle
{
    public static class ReadOnlyAdornmentLayers
    {
        [Export(typeof(AdornmentLayerDefinition))]
        [Name("ReadOnlyAdornmentLayer")]
        [Order(After = PredefinedAdornmentLayers.Selection)]
        public static AdornmentLayerDefinition Layer;
    }
}