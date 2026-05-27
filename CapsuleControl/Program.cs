using System;
using System.Windows.Forms;

namespace CapsuleControl;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (s, e) =>
        {
            Logger.Error("Unhandled UI exception", e.Exception);
            MessageBox.Show(e.Exception.ToString(), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            Logger.Error("Unhandled domain exception", e.ExceptionObject as Exception);
        };

        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }
}
