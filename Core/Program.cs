namespace CockroachPet;

static class Program
{
    /// <summary>
    /// 应用程序的主入口点。
    /// </summary>
    [STAThread]
    static void Main()
    {
        // 捕获所有未处理的异常
        Application.ThreadException += (s, e) => LogException(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (s, e) => LogException(e.ExceptionObject as Exception);

        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }

    static void LogException(Exception? ex)
    {
        if (ex == null) return;
        try
        {
            string logPath = Path.Combine(Path.GetTempPath(), "CockroachPet_Error.log");
            string log = $"[{DateTime.Now}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n";
            File.AppendAllText(logPath, log);
            MessageBox.Show($"程序发生错误:\n{ex.Message}\n\n详细日志已保存到:\n{logPath}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch { }
    }
}
