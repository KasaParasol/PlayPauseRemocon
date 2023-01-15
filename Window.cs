using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlayPauseRemocon
{
    class Window
    {
        static readonly uint WS_EX_TOPMOST = 0x00000008;

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        // (x, y), (cx, cy)を無視するようにする.
        const uint TOPMOST_FLAGS = (SWP_NOSIZE | SWP_NOMOVE);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowInfo(IntPtr hwnd, out WINDOWINFO lpwi);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetLayeredWindowAttributes(IntPtr hwnd, out COLORREF crKey, out byte bAlpha, out uint dwFlags);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private const int GWL_EXSTYLE = -20;
        private const uint WS_EX_TRANSPARENT = 0x00000020;
        private const uint WS_EX_LAYERED = 0x00080000;

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWINFO
        {
            public uint cbSize;
            public RECT rcWindow;
            public RECT rcClient;
            public uint dwStyle;
            public uint dwExStyle;
            public uint dwWindowStatus;
            public uint cxWindowBorders;
            public uint cyWindowBorders;
            public ushort atomWindowType;
            public ushort wCreatorVersion;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct COLORREF
        {
            public uint Colorref;
            public IntPtr lpColorref;
        }

        /// <summary>
        /// ウィンドウタイトル(表示名)
        /// </summary>
        public readonly string title;

        /// <summary>
        /// ウィンドウハンドル
        /// </summary>
        public readonly IntPtr handle;

        public readonly Boolean isTopMost;

        public readonly Boolean isTransparent;

        public Window(string title, IntPtr handle)
        {
            this.title = title;
            this.handle = handle;

            WINDOWINFO info;
            COLORREF colorref;
            byte alpha;
            uint dwFlags;

            GetWindowInfo(handle, out info);
            GetLayeredWindowAttributes(handle, out colorref, out alpha, out dwFlags);
            this.isTopMost = (info.dwExStyle & WS_EX_TOPMOST) != 0;
            this.isTransparent = alpha != 0;
        }

        public void toggleTopMost()
        {
            if (this.isTopMost)
            {
                SetWindowPos(this.handle, HWND_NOTOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
            }
            else
            {
                SetWindowPos(this.handle, HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
            }
        }

        private IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return GetWindowLongPtr32(hWnd, nIndex);
        }

        private IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return SetWindowLongPtr32(hWnd, nIndex, dwNewLong);
        }

        bool AddExStyle(IntPtr handle, int style)
        {
            uint winFlags = (uint)GetWindowLongPtr(handle, GWL_EXSTYLE);

            if (winFlags == 0)
            {
                int error = Marshal.GetLastWin32Error();
                if (error != 0)
                {
                    return false;
                }
            }

            if ((winFlags & WS_EX_LAYERED) == 0)
            {
                winFlags |= WS_EX_LAYERED;
                SetWindowLongPtr(handle, GWL_EXSTYLE, new IntPtr(winFlags));
            }
            return true;
        }

        bool AddTransparetStyle(IntPtr handle)
        {
            uint winFlags = (uint)GetWindowLongPtr(handle, GWL_EXSTYLE);

            if (winFlags == 0)
            {
                int error = Marshal.GetLastWin32Error();
                if (error != 0)
                {
                    return false;
                }
            }

            if ((winFlags & WS_EX_TRANSPARENT) == 0)
            {
                winFlags |= WS_EX_TRANSPARENT;
                SetWindowLongPtr(handle, GWL_EXSTYLE, new IntPtr(winFlags));
            }
            return true;
        }

        bool RestoreTransparetStyle(IntPtr handle)
        {
            uint winFlags = (uint)GetWindowLongPtr(handle, GWL_EXSTYLE);

            if (winFlags == 0)
            {
                int error = Marshal.GetLastWin32Error();
                if (error != 0)
                {
                    return false;
                }
            }

            if ((winFlags & WS_EX_TRANSPARENT & WS_EX_TOPMOST) == 0)
            {
                winFlags ^= (WS_EX_TRANSPARENT | WS_EX_TOPMOST);
                SetWindowLongPtr(handle, GWL_EXSTYLE, new IntPtr(winFlags));
            }
            return true;
        }

        public bool SetOpacity(IntPtr handle, byte alpha)
        {
            // WS_EX_LAYEREDがないなら追加する
            if (!AddExStyle(handle, (int)WS_EX_LAYERED))
                return false;

            if (!SetLayeredWindowAttributes(handle, 0, alpha, 0x2))
            {
                MessageBox.Show("SetLayeredWindowAttributesが失敗");
                return false;
            }
            return true;
        }

        public void toggleTransparent()
        {
            if (this.isTransparent)
            {
                RestoreTransparetStyle(this.handle);
            }
            else
            {
                AddTransparetStyle(this.handle);
            }
        }
    }
}
