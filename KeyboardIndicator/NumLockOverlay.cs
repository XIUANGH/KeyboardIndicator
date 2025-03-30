using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace KeyboardIndicator
{
    public class NumLockOverlay : IDisposable
    {
        private StatusForm statusForm;
        private bool disposed = false;
        private static readonly object syncLock = new object();
        
        // 定义兼容.NET 2.0的委托
        private delegate void NoParamDelegate();
        private delegate void StatusUpdateDelegate(bool isNumLockOn, int displayTimeMs);
        
        public NumLockOverlay()
        {
            // 不在构造函数中创建窗体，延迟到需要时创建
        }
        
        public void ShowStatus(bool isNumLockOn, int displayTimeMs)
        {
            if (disposed) return;
            
            try
            {
                Debug.WriteLine("ShowStatus被调用: NumLock=" + isNumLockOn);
                
                // 在UI线程上创建和显示窗体
                if (statusForm == null || statusForm.IsDisposed)
                {
                    // 创建窗体
                    CreateStatusForm();
                }
                
                // 更新和显示窗体
                if (statusForm != null && !statusForm.IsDisposed)
                {
                    if (statusForm.InvokeRequired)
                    {
                        statusForm.Invoke(new StatusUpdateDelegate(UpdateAndShowForm), 
                            new object[] { isNumLockOn, displayTimeMs });
                    }
                    else
                    {
                        UpdateAndShowForm(isNumLockOn, displayTimeMs);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("NumLockOverlay.ShowStatus错误: " + ex.Message + "\n" + ex.StackTrace);
            }
        }
        
        private void CreateStatusForm()
        {
            try
            {
                lock (syncLock)
                {
                    if (statusForm == null || statusForm.IsDisposed)
                    {
                        statusForm = new StatusForm();
                        Debug.WriteLine("已创建新的StatusForm");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("创建状态窗体时出错: " + ex.Message);
            }
        }
        
        private void UpdateAndShowForm(bool isNumLockOn, int displayTimeMs)
        {
            try
            {
                Debug.WriteLine("更新窗体状态: NumLock=" + isNumLockOn);
                statusForm.SetStatus(isNumLockOn);
                statusForm.ShowForDuration(displayTimeMs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("更新状态窗体时出错: " + ex.Message);
            }
        }
        
        public void Dispose()
        {
            if (!disposed)
            {
                try
                {
                    if (statusForm != null && !statusForm.IsDisposed)
                    {
                        try
                        {
                            if (statusForm.InvokeRequired)
                            {
                                statusForm.Invoke(new NoParamDelegate(CloseStatusForm));
                            }
                            else
                            {
                                CloseStatusForm();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("关闭状态窗体时出错: " + ex.Message);
                        }
                    }
                }
                catch { }
                
                disposed = true;
            }
            
            GC.SuppressFinalize(this);
        }
        
        private void CloseStatusForm()
        {
            try
            {
                statusForm.Close();
                statusForm.Dispose();
                statusForm = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("关闭状态窗体时出错: " + ex.Message);
            }
        }
        
        ~NumLockOverlay()
        {
            Dispose();
        }
        
        // 用于显示NumLock状态的窗体
        private class StatusForm : Form
        {
            private Label statusLabel;
            private System.Windows.Forms.Timer closeTimer;
            
            public StatusForm()
            {
                InitializeComponents();
                this.Shown += (s, e) => this.SetWindowPos();
            }
            
            private void InitializeComponents()
            {
                try
                {
                    // 基本窗体设置
                    this.FormBorderStyle = FormBorderStyle.None;
                    this.StartPosition = FormStartPosition.CenterScreen;
                    this.ShowInTaskbar = false;
                    this.TopMost = true;
                    this.Size = new Size(180, 60);
                    this.BackColor = Color.Black;
                    this.Opacity = 0.8;
                    
                    // 状态标签设置
                    statusLabel = new Label();
                    statusLabel.Dock = DockStyle.Fill;
                    statusLabel.Font = new Font("微软雅黑", 16F, FontStyle.Bold);
                    statusLabel.ForeColor = Color.White;
                    statusLabel.TextAlign = ContentAlignment.MiddleCenter;
                    statusLabel.BackColor = Color.Transparent;
                    statusLabel.Text = "初始化...";
                    this.Controls.Add(statusLabel);
                    
                    // 初始化定时器
                    closeTimer = new System.Windows.Forms.Timer();
                    closeTimer.Interval = 1000;
                    closeTimer.Tick += (s, e) => {
                        Debug.WriteLine("计时器触发，隐藏窗体");
                        closeTimer.Stop();
                        this.Hide();
                    };
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("初始化窗体组件错误: " + ex.Message);
                }
            }
            
            // 设置窗体显示的状态
            public void SetStatus(bool isNumLockOn)
            {
                try
                {
                    statusLabel.Text = isNumLockOn ? "NumLock: 开启" : "NumLock: 关闭";
                    statusLabel.ForeColor = isNumLockOn ? Color.LightGreen : Color.LightCoral;
                    Debug.WriteLine("设置状态文本: " + statusLabel.Text);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("设置状态文本时出错: " + ex.Message);
                }
            }
            
            // 显示窗体一段时间后自动关闭
            public void ShowForDuration(int milliseconds)
            {
                try
                {
                    // 停止现有定时器
                    closeTimer.Stop();
                    closeTimer.Interval = milliseconds;
                    
                    // 显示窗体
                    this.Show();
                    this.Refresh();
                    
                    // 确保窗体置顶
                    this.BringToFront();
                    this.SetWindowPos();
                    
                    Debug.WriteLine("窗体显示，设置计时器: " + milliseconds + "ms");
                    
                    // 启动定时器
                    closeTimer.Start();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("显示窗体时出错: " + ex.Message);
                }
            }
            
            // 设置窗体置顶
            private void SetWindowPos()
            {
                try
                {
                    NativeMethods.SetWindowPos(
                        this.Handle, 
                        NativeMethods.HWND_TOPMOST, 
                        0, 0, 0, 0, 
                        NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_SHOWWINDOW);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("设置窗口位置时出错: " + ex.Message);
                }
            }
            
            // 设置窗口样式
            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.ExStyle |= 0x00000080 | 0x00000008 | 0x08000000;
                    return cp;
                }
            }
        }
    }
    
    // Windows API 调用封装
    internal static class NativeMethods
    {
        public const int SWP_NOMOVE = 0x0002;
        public const int SWP_NOSIZE = 0x0001;
        public const int SWP_SHOWWINDOW = 0x0040;
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    }
}
