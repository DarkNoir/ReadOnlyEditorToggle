using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace ReadOnlyEditorToggle
{
    internal sealed class ToggleReadonlyCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("e558ccf6-fbdd-415a-8c41-148034a51fcc");

        private readonly AsyncPackage _package;

        private ToggleReadonlyCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));

            CommandID cmdId = new CommandID(CommandSet, CommandId);
            MenuCommand menuItem = new MenuCommand(Execute, cmdId);
            commandService.AddCommand(menuItem);
        }

        public static ToggleReadonlyCommand Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new ToggleReadonlyCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _package.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (!(await _package.GetServiceAsync(typeof(SVsTextManager)) is IVsTextManager textManager))
                    return;

                _ = textManager.GetActiveView(1, null, out IVsTextView vsTextView);
                if (vsTextView == null)
                    return;

                if (!(await _package.GetServiceAsync(typeof(SComponentModel)) is IComponentModel componentModel))
                    return;

                var adapter = componentModel.GetService<IVsEditorAdaptersFactoryService>();
                IWpfTextView view = adapter.GetWpfTextView(vsTextView);
                if (view == null)
                    return;

                var controller = view.Properties.GetOrCreateSingletonProperty(
                    () => new ReadOnlyAdornmentController(view, adapter));

                controller.Toggle();

            }).FileAndForget("ReadOnlyEditorToggle/ToggleCommand");
        }
    }
}