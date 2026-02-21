using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

using System;
using System.ComponentModel.Design;

using Task = System.Threading.Tasks.Task;

namespace ReadOnlyEditorToggle
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ToggleReadonlyCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("e558ccf6-fbdd-415a-8c41-148034a51fcc");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage _Package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToggleReadonlyCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private ToggleReadonlyCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this._Package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            CommandID menuCommandID = new CommandID(CommandSet, CommandId);
            OleMenuCommand menuItem = new OleMenuCommand(this.Execute, menuCommandID);
            menuItem.BeforeQueryStatus += OnBeforeQueryStatus;
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ToggleReadonlyCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this._Package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in ToggleReadonlyCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            System.Diagnostics.Debug.Assert(commandService != null, "CommandService is null");
            Instance = new ToggleReadonlyCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _Package.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Get the text manager
                if (!(await ServiceProvider.GetServiceAsync(typeof(SVsTextManager)) is IVsTextManager textManager))
                    return;

                // Get the active VS text view
                _ = textManager.GetActiveView(1, null, out IVsTextView vsTextView);
                if (vsTextView == null)
                    return;

                // Get the component model (this is your missing piece)
                if (!(await ServiceProvider.GetServiceAsync(typeof(SComponentModel)) is IComponentModel componentModel))
                    return;

                // Get the editor adapter service
                IVsEditorAdaptersFactoryService editorAdapter = componentModel.GetService<IVsEditorAdaptersFactoryService>();

                // Convert to WPF text view
                IWpfTextView textView = editorAdapter.GetWpfTextView(vsTextView);
                if (textView == null)
                    return;

                // Get the buffer
                ITextBuffer buffer = textView.TextBuffer;

                // Toggle read-only
                ReadOnlyController controller = ReadOnlyController.GetOrCreate(buffer, editorAdapter);
                controller.Toggle();
            }).FileAndForget("EditorLockToggleEvent", "LockToggleFault", (ex) => { return true; });
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            OleMenuCommand command = sender as OleMenuCommand;
            if (command == null)
                return;

            // Default: disabled + unchecked
            command.Enabled = false;
            command.Checked = false;

            // Try to get the active VS text view
            IVsTextManager textManager = _Package.GetServiceAsync(typeof(SVsTextManager)).Result as IVsTextManager;
            if(textManager == null)
                return;

            _ = textManager.GetActiveView(1, null, out IVsTextView vsTextView);
            if (vsTextView == null)
                return;

            // Get component model
            IComponentModel componentModel = _Package.GetServiceAsync(typeof(SComponentModel)).Result as IComponentModel;
            if (componentModel == null)
                return;

            IVsEditorAdaptersFactoryService editorAdapter = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            IWpfTextView textView = editorAdapter.GetWpfTextView(vsTextView);

            // If no WPF text view, this is not a text buffer tab
            if (textView == null)
                return;

            // We have a valid text buffer → enable the command
            command.Enabled = true;

            ITextBuffer buffer = textView.TextBuffer;

            // If controller exists, reflect its state
            if (buffer.Properties.TryGetProperty(typeof(ReadOnlyController), out ReadOnlyController controller))
                command.Checked = controller.IsLocked;
        }
    }
}
