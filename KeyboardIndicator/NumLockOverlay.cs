using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

namespace KeyboardIndicator
{
    public class NumLockOverlay : IDisposable
    {
        private bool disposed = false;
        private static readonly object syncLock = new object();
        private Form displayForm = null;
        
        public NumLockOverlay()
        {
            // 不预先创建窗体
        }
        
        public void ShowStatus(bool isNumLockOn, int displayTimeMs)
        {
            if (disposed) return;
            
            try
            {
                Debug.WriteLine("ShowStatus被调用: NumLock=" + isNumLockOn);
                
                // 创建新窗体并在新线程中显示
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object state) {
                    try {
                        ShowStatusWindow(isNumLockOn, displayTimeMs);
                    }
                    catch (Exception ex) {
                        Debug.WriteLine("线程池显示窗体错误: " + ex.Message);
                    }
                }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("NumLockOverlay.ShowStatus错误: " + ex.Message);
            }
        }
        
        private void ShowStatusWindow(bool isNumLockOn, int displayTimeMs)
        {
            // 检查现有窗体并关闭
            if (displayForm != null)
            {
                try
                {
                    if (!displayForm.IsDisposed)
                    {
                        displayForm.Invoke(new MethodInvoker(delegate {
                            try { displayForm.Close(); } catch { }
                        }));
                    }
                }
                catch { }
                displayForm = null;
            }
            
            // 创建新窗体实例
            Form statusForm = new Form();
            displayForm = statusForm;
            
            // 配置窗体
            statusForm.FormBorderStyle = FormBorderStyle.None;
            statusForm.StartPosition = FormStartPosition.CenterScreen;
            statusForm.ShowInTaskbar = false;
            statusForm.TopMost = true;
            statusForm.Size = new Size(180, 60);
            statusForm.BackColor = Color.Black;
            statusForm.Opacity = 0.8;
            
            // 创建状态标签
            Label statusLabel = new Label();
            statusLabel.Dock = DockStyle.Fill;
            statusLabel.Font = new Font("微软雅黑", 16F, FontStyle.Bold);
            statusLabel.ForeColor = isNumLockOn ? Color.LightGreen : Color.LightCoral;
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusLabel.BackColor = Color.Transparent;
            statusLabel.Text = isNumLockOn ? "NumLock: 开启" : "NumLock: 关闭";
            statusForm.Controls.Add(statusLabel);
            
            Debug.WriteLine("准备显示新窗体 - 线程ID: " + Thread.CurrentThread.ManagedThreadId);
            
            // 设置计时器关闭窗体
            System.Windows.Forms.Timer closeTimer = new System.Windows.Forms.Timer();
            closeTimer.Interval = displayTimeMs;
            closeTimer.Tick += delegate(object sender, EventArgs e) {
                Debug.WriteLine("计时器触发，关闭窗体");
                closeTimer.Stop();
                statusForm.Close();
                displayForm = null;
            };
            
            // 显示窗体并启动消息循环
            statusForm.Load += delegate(object sender, EventArgs e) {
                // 设置窗体位置
                SetWindowPosition(statusForm);
                // 启动计时器
                closeTimer.Start();
                Debug.WriteLine("窗体加载完成，计时器启动: " + displayTimeMs + "ms");
            };
            
            // 这是关键 - 运行独立的消息循环
            Application.Run(statusForm);
            
            Debug.WriteLine("窗体消息循环结束");
        }
        
        private void SetWindowPosition(Form form)
        {
            // 确保窗体置顶显示
            form.BringToFront();
            
            // 使用API设置窗体为顶层
            NativeMethods.SetWindowPos(
                form.Handle,
                (IntPtr)NativeMethods.HWND_TOPMOST,
                (Screen.PrimaryScreen.Bounds.Width - form.Width) / 2,
                (Screen.PrimaryScreen.Bounds.Height - form.Height) / 2,
                form.Width,
                form.Height,
                NativeMethods.SWP_SHOWWINDOW
            );
        }
        
        public void Dispose()
        {
            if (!disposed)
            {
                try
                {
                    if (displayForm != null && !displayForm.IsDisposed)
                    {
                        try
                        {
                            displayForm.Invoke(new MethodInvoker(delegate {
                                displayForm.Close();
                            }));
                        }
                        catch { }
                        displayForm = null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NumLockOverlay.Dispose错误: " + ex.Message);
                }
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }
        
        ~NumLockOverlay()
        {
            Dispose();
        }
    }
    
    internal static class NativeMethods
    {
        public const int HWND_TOPMOST = -1;
        public const int SWP_SHOWWINDOW = 0x0040;
        
        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    }
}
