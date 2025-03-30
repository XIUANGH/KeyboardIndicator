using KeyboardIndicator.Properties;
using System;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Drawing;
using System.Timers;
using Timer = System.Windows.Forms.Timer;

namespace KeyboardIndicator
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;
    using System.Runtime.InteropServices;
    using System.Diagnostics;
    public class AutoClosingMessageBox
    {
        public System.Threading.Timer _timeoutTimer;
        public string _caption;
        public AutoClosingMessageBox()
        {
        }
        //弹窗显示时长timeout默认值设置的是3000ms(也就是3秒)
        public AutoClosingMessageBox(string text, string caption, int timeout = 3000)
        {
            _caption = caption;
            _timeoutTimer = new System.Threading.Timer(OnTimerElapsed, null, timeout, System.Threading.Timeout.Infinite);
            MessageBox.Show(text, caption, MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
        }
        /// <summary>
        /// 将指定内容弹窗显示一定时长后自动关闭窗口
        /// </summary>
        /// <param name="text">显示内容</param>
        /// <param name="caption">说明内容</param>
        /// <param name="timeout">显示时长（毫秒）</param>
        public void Show(string text, string caption, int timeout)
        {
            new AutoClosingMessageBox(text, caption, timeout);
        }
        public void Show(string text, string caption)
        {
            new AutoClosingMessageBox(text, caption);
        }
        public void OnTimerElapsed(object state)
        {
            IntPtr mbWnd = FindWindow(null, _caption);
            if (mbWnd != IntPtr.Zero)
                SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            _timeoutTimer.Dispose();
        }
        const int WM_CLOSE = 0x0010;
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
    }

    
    public partial class KeyboardIndicator : Form
    {
        public delegate int HookProc(int nCode, int wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public class KeyBoardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 256;

        private const int NUM_KEYCODE = 144;//Num Lock键：VK_NUMLOCK (144)
        private const int CAPS_KEYCODE = 20;//Caps Lock键：VK_CAPITAL (20)

        private KeyboardIndicator.KeyBoardHookStruct kbh;
        private KeyboardIndicator.HookProc gHookProc;

        private int curCount;
        private bool isRunning;

        [StructLayout(LayoutKind.Explicit)]
        public struct INPUT
        {
            [FieldOffset(0)]
            public int type;
            [FieldOffset(4)]
            public KEYBDINPUT ki;
            [FieldOffset(4)]
            public MOUSEINPUT mi;
            [FieldOffset(4)]
            public HARDWAREINPUT hi;
        }
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public int mouseData;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        }
        public struct KEYBDINPUT
        {
            public short wVk;
            public short wScan;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        }
        public struct HARDWAREINPUT
        {
            public int uMsg;
            public short wParamL;
            public short wParamH;
        }

        [DllImport("user32")]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        public static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        public static extern int SetWindowsHookEx(int idHook, KeyboardIndicator.HookProc lpfn, IntPtr hInstance, int threadId);

        private bool showNumLock = false;
        private bool showCapsLock = false;

        private ContextMenu trayMenu;

        //private Label statusLabel;
        //private Timer disappearTimer;
        private NumLockOverlay numLockOverlay;
        private IntPtr hookID = IntPtr.Zero;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, int wParam, IntPtr lParam);

        public KeyboardIndicator()
        {
            InitializeComponent();
            trayMenu = new ContextMenu();

            bool currentStartupStatus = Properties.Settings.Default.Startup;
            string startUpStatus = currentStartupStatus ? "已开启" : "未开启";
            
            trayMenu.MenuItems.Add("开机自启动状态:" + startUpStatus, ToggleStartup);

            // 将右键菜单关联到通知图标
            notifyIconNUM.ContextMenu = trayMenu;
            notifyIconCAPS.ContextMenu = trayMenu;
            
            try
            {
                showNumLock = ConfigurationManager.AppSettings["NumLock"].ToUpper() == "Y";
            }
            catch (Exception) 
            {
                showNumLock = false;
            }

            try
            {
                showCapsLock = ConfigurationManager.AppSettings["CapsLock"].ToUpper() == "Y";
            }
            catch (Exception) 
            {
                showCapsLock = false;
            }

            if (!showNumLock && !showCapsLock)
            {
                showNumLock = true;
            }

            // 在设置钩子前初始化Overlay
            InitializeOverlay();
            
            this.SetStatus();
            this.gHookProc = new KeyboardIndicator.HookProc(this.KeyBoardHookProc);
            KeyboardIndicator.SetWindowsHookEx(WH_KEYBOARD_LL, this.gHookProc, IntPtr.Zero, 0);
        }

        public void SimulateInputKey(int key)
        {
            INPUT[] input = new INPUT[1];

            input[0].type = 1;//模拟键盘
            input[0].ki.wVk = (short)key;
            input[0].ki.dwFlags = 0;//按下
            uint res1= SendInput(1u, input, Marshal.SizeOf((object)default(INPUT)));
            
            System.Threading.Thread.Sleep(200);

            input[0].type = 1;//模拟键盘
            input[0].ki.wVk = (short)key;
            input[0].ki.dwFlags = 2;//抬起
            uint res2=SendInput(1u, input, Marshal.SizeOf((object)default(INPUT)));
            MessageBox.Show(res1.ToString()+res2.ToString());
        }

        private void ToggleStartup(object sender, EventArgs e)
        {
            bool currentStartupStatus = Properties.Settings.Default.Startup; // 获取当前开机自启动状态

            SetStartup(!currentStartupStatus); // 反转状态

            Properties.Settings.Default.Startup = !currentStartupStatus; // 更新设置值
            Properties.Settings.Default.Save(); // 保存设置值

            UpdateStartupStatusMenu(); // 更新菜单项显示状态
        }

        private void UpdateStartupStatusMenu()
        {
            bool currentStartupStatus = Properties.Settings.Default.Startup; // 获取当前开机自启动状态

            // 更新菜单项显示状态
            trayMenu.MenuItems[0].Text = "开机自启动状态: " + (currentStartupStatus ? "已开启" : "未开启");
        }

        public void SetStartup(bool enable)
        {
            string runKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
            RegistryKey startupKey = Registry.CurrentUser.OpenSubKey(runKey, true);

            if (enable)
            {
                if (startupKey.GetValue("KeyboardIndicator") == null)
                {
                    startupKey.SetValue("KeyboardIndicator", System.Reflection.Assembly.GetExecutingAssembly().Location);
                }
            }
            else
            {
                startupKey.DeleteValue("KeyboardIndicator", false);
            }
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Close();
            this.Dispose(true);
            Application.ExitThread();
        }

        private void notifyIcon_MouseSingleClick(object sender, MouseEventArgs e)
        {
            NotifyIcon icon=sender as NotifyIcon;
            //SimulateInputKey(CAPS_KEYCODE);
            //MessageBox.Show(sender.ToString()+ e.ToString()+ icon.Text, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void InitializeOverlay()
        {
            try
            {
                numLockOverlay = new NumLockOverlay();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("无法初始化NumLockOverlay: " + ex.Message);
            }
        }

        private void KeyboardIndicator_Load(object sender, EventArgs e)
        {
            // 不再需要在这里初始化
            // InitializeOverlay();
        }

        public int KeyBoardHookProc(int nCode, int wParam, IntPtr lParam)
        {
            try
            {
                this.kbh = (KeyboardIndicator.KeyBoardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardIndicator.KeyBoardHookStruct));
                if (!this.isRunning && (this.kbh.vkCode == NUM_KEYCODE || this.kbh.vkCode == CAPS_KEYCODE))
                {
                    bool isNumLockKey = this.kbh.vkCode == NUM_KEYCODE;
                    
                    ThreadPool.QueueUserWorkItem(delegate (object param0)
                    {
                        this.isRunning = true;
                        this.curCount = 200;
                        
                        if (isNumLockKey)
                        {
                            // 记录初始NumLock状态
                            bool initialNumLockState = Control.IsKeyLocked(Keys.NumLock);
                            bool stateChanged = false;
                            bool lastState = initialNumLockState;
                            
                            // 使用原有的循环监控状态变化
                            for (int i = 0; i < this.curCount; i++)
                            {
                                // 更新系统托盘图标状态
                                this.SetStatus();
                                
                                // 检查当前NumLock状态
                                bool currentState = Control.IsKeyLocked(Keys.NumLock);
                                
                                // 检测到状态变化
                                if (currentState != lastState)
                                {
                                    lastState = currentState;
                                    stateChanged = true;
                                    
                                    try
                                    {
                                        // 显示状态变化
                                        if (numLockOverlay != null)
                                        {
                                            numLockOverlay.ShowStatus(currentState, 1000);
                                        }
                                        Debug.WriteLine("显示NumLock状态: " + currentState);
                                        Debug.WriteLine(numLockOverlay==null);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine("显示NumLock状态时出错: " + ex.Message);
                                    }
                                }
                                
                                Thread.Sleep(20);
                                this.curCount--;
                            }
                        }
                        else // CapsLock处理
                        {
                            // 保持原有逻辑
                            while (this.curCount > 0)
                            {
                                this.SetStatus();
                                Thread.Sleep(20);
                                this.curCount--;
                            }
                        }
                        
                        this.curCount = 0;
                        this.isRunning = false;
                    });
                }
                return (int)CallNextHookEx(this.hookID, nCode, wParam, lParam);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("KeyBoardHookProc error: " + ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return (int)CallNextHookEx(this.hookID, nCode, wParam, lParam);
            }
        }

        private void SetStatus()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker(SetStatus));
                return;
            }
            this.notifyIconNUM.Visible = showNumLock;
            this.notifyIconCAPS.Visible = showCapsLock;

            if (showNumLock)
            {
                if (GetKeyState(NUM_KEYCODE) != 0)
                {
                    this.notifyIconNUM.Icon = Resources.NumLockOpen;
                    this.notifyIconNUM.Text = "NumLock On";
                }
                else
                {
                    this.notifyIconNUM.Icon = Resources.NumLockClose;
                    this.notifyIconNUM.Text = "NumLock Off";
                }
            }

            if (showCapsLock)
            {
                if (GetKeyState(CAPS_KEYCODE) != 0)
                {
                    this.notifyIconCAPS.Icon = Resources.CapsLockOpen;
                    this.notifyIconCAPS.Text = "CapsLock On";
                }
                else
                {
                    this.notifyIconCAPS.Icon = Resources.CapsLockClose;
                    this.notifyIconCAPS.Text = "CapsLock Off";
                }
            }
        }

    }
}
