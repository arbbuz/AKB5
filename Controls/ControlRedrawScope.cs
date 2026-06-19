using System.Runtime.InteropServices;

namespace AsutpKnowledgeBase
{
    internal sealed class ControlRedrawScope : IDisposable
    {
        private const int WM_SETREDRAW = 0x000B;

        private readonly Control _control;
        private readonly bool _isActive;
        private bool _isDisposed;

        private ControlRedrawScope(Control control)
        {
            _control = control;
            _isActive = !control.IsDisposed && control.IsHandleCreated;

            if (_isActive)
                SendMessage(control.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        }

        public static ControlRedrawScope Suspend(Control control) => new(control);

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            if (!_isActive || _control.IsDisposed)
                return;

            SendMessage(_control.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
            _control.Invalidate(true);
            _control.Update();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}
