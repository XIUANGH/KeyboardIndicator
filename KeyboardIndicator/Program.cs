using System;
using System.Threading;
using System.Windows.Forms;
namespace KeyboardIndicator
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool flag;
            using (new Mutex(true, "KeyboardIndicator", out flag))
            {
                if (flag)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
                    AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
                    Form myform = new KeyboardIndicator();
                    Application.Run();

                }
            }
        }
        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, "Unhandled Thread Exception");
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show((e.ExceptionObject as Exception).Message, "Unhandled UI Exception");
        }
    }
}
