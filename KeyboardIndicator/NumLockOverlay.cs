using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace KeyboardIndicator
{
    public class NumLockOverlay : IDisposable
    {
        private bool disposed = false;
        private IntPtr hwnd = IntPtr.Zero;
        private System.Threading.Timer closeTimer;
        
        public NumLockOverlay()
        {
        }
        
        public void ShowStatus(bool isNumLockOn, int displayTimeMs)
        {
            if (disposed) return;
            
            try
            {
                string text = isNumLockOn ? "NumLock: ON" : "NumLock: OFF";
                
                // 创建关闭消息定时器
                if (closeTimer != null)
                {
                    closeTimer.Dispose();
                }
                closeTimer = new System.Threading.Timer(CloseMessageCallback, null, displayTimeMs, System.Threading.Timeout.Infinite);
                
                // 显示系统消息
                string title = "NumLock Status";
                MessageBeep(0); // 播放默认声音
                
                // 转换以确保标题和内容的编码正确
                byte[] titleBytes = Encoding.Default.GetBytes(title);
                byte[] textBytes = Encoding.Default.GetBytes(text);
                string encodedTitle = Encoding.Default.GetString(titleBytes);
                string encodedText = Encoding.Default.GetString(textBytes);
                
                // 显示消息窗口
                hwnd = FindWindow(null, encodedTitle);
                if (hwnd != IntPtr.Zero)
                {
                    // 关闭之前的窗口
                    SendMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }
                
                ShowTipWindow(encodedTitle, encodedText);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("NumLockOverlay.ShowStatus错误: " + ex.Message);
            }
        }
        
        private void CloseMessageCallback(object state)
        {
            try
            {
                string title = "NumLock Status";
                byte[] titleBytes = Encoding.Default.GetBytes(title);
                string encodedTitle = Encoding.Default.GetString(titleBytes);
                
                IntPtr hwnd = FindWindow(null, encodedTitle);
                if (hwnd != IntPtr.Zero)
                {
                    SendMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("关闭消息窗口时出错: " + ex.Message);
            }
        }
        
        private void ShowTipWindow(string title, string text)
        {
            try
            {
                int timeout = 1; // 1秒超时
                // 使用MB_TOPMOST标志确保窗口置顶 (0x00040000)
                uint flags = 0x00040000 | 0x00000000; // MB_TOPMOST | MB_OK
                MessageBoxTimeout(IntPtr.Zero, text, title, flags, 0, (uint)(timeout * 1000));
                
                // 找到消息窗口并确保它位于前台
                System.Threading.Thread.Sleep(50); // 短暂延迟确保窗口创建
                IntPtr hwnd = FindWindow(null, title);
                if (hwnd != IntPtr.Zero)
                {
                    // 设置窗口为前台窗口 
                    SetForegroundWindow(hwnd);
                    
                    // 设置窗口置顶
                    SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, 
                        SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ShowTipWindow错误: " + ex.Message);
            }
        }
        
        public void Dispose()
        {
            if (!disposed)
            {
                try
                {
                    if (closeTimer != null)
                    {
                        closeTimer.Dispose();
                        closeTimer = null;
                    }
                    
                    if (hwnd != IntPtr.Zero)
                    {
                        SendMessage(hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                        hwnd = IntPtr.Zero;
                    }
                }
                catch { }
                
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }
        
        ~NumLockOverlay()
        {
            Dispose();
        }
        
        // Windows API
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool MessageBeep(uint uType);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int MessageBoxTimeout(IntPtr hwnd, String text, String title, 
            uint type, uint wLanguageId, uint dwMilliseconds);
            
        private const UInt32 WM_CLOSE = 0x0010;
        
        // 添加必要的Windows API声明
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_SHOWWINDOW = 0x0040;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    }
} 