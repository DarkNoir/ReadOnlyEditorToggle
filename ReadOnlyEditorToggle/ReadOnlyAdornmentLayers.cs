using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

using System.ComponentModel.Composition;

namespace ReadOnlyEditorToggle
{
    internal static class ReadOnlyAdornmentLayers
    {
        [Export(typeof(AdornmentLayerDefinition))]
        [Name("ReadOnlyBorderAdornmentLayer")]
        [Order(After = PredefinedAdornmentLayers.Selection)]
        public static AdornmentLayerDefinition BorderLayer = null;

        [Export(typeof(AdornmentLayerDefinition))]
        [Name("ReadOnlyTintAdornmentLayer")]
        [Order(Before = PredefinedAdornmentLayers.Text)]
        public static AdornmentLayerDefinition TintLayer = null;
    }
}