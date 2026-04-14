
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GUI
{
    internal static class Program
    {
        // Prefer per-monitor v2 when available (Windows 10+)
        [DllImport("user32.dll", EntryPoint = "SetProcessDpiAwarenessContext")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

        // Fallback older API
        [DllImport("user32.dll", EntryPoint = "SetProcessDPIAware")]
        private static extern bool SetProcessDPIAware();

        // Common DPI_AWARENESS_CONTEXT values
        private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        [STAThread]
        static void Main()
        {
            // Try to set best DPI awareness available, but do it safely and only once.
            try
            {
                // Try per-monitor v2 first (if OS supports it)
                try
                {
                    if (SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2))
                    {
                        // success
                    }
                }
                catch (EntryPointNotFoundException)
                {
                    // OS doesn't expose SetProcessDpiAwarenessContext -> fallback
                    try { SetProcessDPIAware(); } catch { }
                }
                catch
                {
                    // fallback to older API
                    try { SetProcessDPIAware(); } catch { }
                }
            }
            catch
            {
                // swallow all: don't crash app just for DPI
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());
        }
    }
}