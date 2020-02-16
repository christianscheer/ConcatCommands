using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace ConcatCommands
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(ConcatCommandsPackage.PACKAGE_GUID_STRING)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class ConcatCommandsPackage : AsyncPackage, IOleCommandTarget
    {
        /// <summary>
        /// ConcatCommandsPackage GUID string.
        /// </summary>
        public const string PACKAGE_GUID_STRING = "83b436fd-f821-4168-b97c-8050f736ea6a";
        private IOleCommandTarget _pkgCommandTarget;

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _pkgCommandTarget = await this.GetServiceAsync(typeof(IOleCommandTarget)) as IOleCommandTarget;
        }

        #endregion

        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (pguidCmdGroup == ConcatCommand.CommandSet)
            {
                switch (prgCmds[0].cmdID)
                {
                    case ConcatCommand.CommandId:
                        prgCmds[0].cmdf |= (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_INVISIBLE);
                        return VSConstants.S_OK;
                }
            }

            return this._pkgCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (pguidCmdGroup == ConcatCommand.CommandSet)
            {
                switch (nCmdID)
                {
                    case ConcatCommand.CommandId:
                        if (IsQueryParameterList(pvaIn, pvaOut, nCmdexecopt))
                        {
                            Marshal.GetNativeVariantForObject("p", pvaOut);
                            return VSConstants.S_OK;
                        }
                        else
                        {
                            // no args
                            if (pvaIn == IntPtr.Zero)
                                return VSConstants.S_FALSE;

                            object vaInObject = Marshal.GetObjectForNativeVariant(pvaIn);
                            if (vaInObject == null || vaInObject.GetType() != typeof(string))
                                return VSConstants.E_INVALIDARG;

                            if ((vaInObject is string argCommand) && !string.IsNullOrEmpty(argCommand))
                            {
                                var dte = GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                                if (dte != null)
                                {
                                    argCommand
                                        ?.Split(new[] { "&&" }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(s => s?.Trim())
                                        .Select(s => s?.Split(' '))
                                        .Where(ar => ar != null && ar.Length > 0)
                                        .Select(ar => new
                                        {
                                            cmd = ar.FirstOrDefault(),
                                            args = string.Join(" ", ar.Skip(1))
                                        })
                                        .Where(a => !string.IsNullOrWhiteSpace(a.cmd))
                                        .ToList()
                                        .ForEach(a => dte.ExecuteCommand(a.cmd, a.args));
                                }
                            }
                        }
                        return VSConstants.S_OK;
                }
            }

            return this._pkgCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        private static bool IsQueryParameterList(IntPtr pvaIn, IntPtr pvaOut, uint nCmdexecopt)
        {
            ushort lo = (ushort)(nCmdexecopt & (uint)0xffff);
            ushort hi = (ushort)(nCmdexecopt >> 16);
            if (lo == (ushort)OLECMDEXECOPT.OLECMDEXECOPT_SHOWHELP)
            {
                if (hi == VsMenus.VSCmdOptQueryParameterList)
                {
                    return true;
                }
            }

            return false;
        }

        internal sealed class ConcatCommand
        {
            public const int CommandId = 0x0100;
            public static readonly Guid CommandSet = new Guid("096073bc-cd00-4cf1-975d-f007a33fba64");
        }
    }
}
