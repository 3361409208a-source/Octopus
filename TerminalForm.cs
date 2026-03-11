using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CockroachPet;

public class TerminalForm : Form
{
    private Robot _robot;
    private Process? _cmdProcess;
    private IntPtr _cmdHwnd = IntPtr.Zero;
    private NotifyIcon? _trayIcon;
    private bool _allowClose = false;
    private System.Windows.Forms.Timer? _monitorTimer;

    // Win32 API
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int SW_RESTORE = 9;
    private const int GWL_WNDPROC = -4;
    private const uint WM_CLOSE = 0x0010;

    private IntPtr _originalWndProc = IntPtr.Zero;
    private WndProcDelegate? _newWndProc;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    public TerminalForm(Robot robot)
    {
        _robot = robot;
        InitializeComponent();
        InitializeTrayIcon();
        StartCmdProcess();
        StartMonitoring();
    }

    private void InitializeComponent()
    {
        // 这个窗口只用于管理，不显示界面
        this.Text = $"🤖 {_robot.Name} - Terminal Manager";
        this.Size = new Size(1, 1);
        this.StartPosition = FormStartPosition.Manual;
        this.Location = new Point(-10000, -10000);
        this.ShowInTaskbar = false;
        this.FormBorderStyle = FormBorderStyle.None;
        this.Opacity = 0;

        this.FormClosing += TerminalForm_FormClosing;
    }

    private void InitializeTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Text = $"🤖 {_robot.Name} - Terminal",
            Icon = SystemIcons.Application,
            Visible = false  // 不显示托盘图标
        };
    }

    private void StartCmdProcess()
    {
        try
        {
            // 创建临时批处理文件
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RobotTerminals");
            System.IO.Directory.CreateDirectory(tempDir);
            string batchFile = System.IO.Path.Combine(tempDir, $"robot_{_robot.Id}.bat");
            string statusFile = System.IO.Path.Combine(tempDir, $"robot_{_robot.Id}_status.txt");

            // 保存状态文件
            UpdateStatusFile(statusFile);

            // 创建批处理文件
            string windowTitle = $"Robot_{_robot.Id}_{_robot.Name}";
            string batchContent = "@echo off\n" +
                "chcp 65001 >nul 2>&1\n" +
                $"title {windowTitle}\n" +
                "color 0A\n" +
                "cls\n" +
                "echo ==========================================\n" +
                $"echo  Robot: {_robot.Name}    ID: {_robot.Id:D3}\n" +
                "echo ==========================================\n" +
                "echo.\n" +
                "echo Robot Commands:\n" +
                "echo   robot-name    - Show robot name\n" +
                "echo   robot-status  - Show robot status\n" +
                "echo   robot-resume  - Resume robot movement\n" +
                "echo   robot-stop    - Stop robot movement\n" +
                "echo.\n" +
                "echo Type 'exit' to close terminal\n" +
                "echo ==========================================\n" +
                "echo.\n" +
                "doskey robot-name=echo Name: " + _robot.Name + " ^& echo ID: " + _robot.Id + "\n" +
                "doskey robot-status=type \"" + statusFile + "\" 2^>nul ^|^| echo Status file not found\n" +
                "doskey robot-resume=echo Robot movement resumed\n" +
                "doskey robot-stop=echo Robot movement stopped\n" +
                "\n" +
                $"prompt [{_robot.Name}]$G \n" +
                "cmd /k\n";
            System.IO.File.WriteAllText(batchFile, batchContent);

            // 启动 CMD
            _cmdProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{batchFile}\"",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal
                },
                EnableRaisingEvents = true
            };

            _cmdProcess.Start();

            // 等待窗口创建并获取句柄
            System.Threading.Tasks.Task.Delay(800).ContinueWith(_ =>
            {
                this.Invoke(new Action(() =>
                {
                    FindCmdWindow(windowTitle);
                    if (_cmdHwnd != IntPtr.Zero)
                    {
                        HookCmdWindow();
                    }
                }));
            });

            _cmdProcess.Exited += (s, e) =>
            {
                // CMD进程退出时，只是标记为已退出
                this.Invoke(new Action(() =>
                {
                    _cmdHwnd = IntPtr.Zero;
                    _cmdProcess = null;
                    UnhookCmdWindow();
                }));
            };
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start terminal: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void FindCmdWindow(string windowTitle)
    {
        // 查找 CMD 窗口句柄
        for (int i = 0; i < 20 && _cmdHwnd == IntPtr.Zero; i++)
        {
            System.Threading.Thread.Sleep(100);
            _cmdHwnd = FindWindow(null, windowTitle);
        }

        if (_cmdHwnd == IntPtr.Zero && _cmdProcess != null)
        {
            // 尝试通过进程主窗口句柄
            _cmdProcess.Refresh();
            _cmdHwnd = _cmdProcess.MainWindowHandle;
        }
    }

    private void HookCmdWindow()
    {
        if (_cmdHwnd == IntPtr.Zero) return;

        try
        {
            // 创建新的窗口过程委托
            _newWndProc = new WndProcDelegate(CmdWindowProc);
            
            // 保存原始窗口过程并设置新的
            _originalWndProc = GetWindowLongPtr(_cmdHwnd, GWL_WNDPROC);
            SetWindowLongPtr(_cmdHwnd, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_newWndProc));
        }
        catch
        {
            // Hook失败，使用监控方式
        }
    }

    private void UnhookCmdWindow()
    {
        if (_cmdHwnd != IntPtr.Zero && _originalWndProc != IntPtr.Zero)
        {
            try
            {
                SetWindowLongPtr(_cmdHwnd, GWL_WNDPROC, _originalWndProc);
            }
            catch { }
        }
        _originalWndProc = IntPtr.Zero;
        _newWndProc = null;
    }

    private IntPtr CmdWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        // 拦截关闭消息
        if (msg == WM_CLOSE)
        {
            // 隐藏窗口而不是关闭
            ShowWindow(hWnd, SW_HIDE);
            return IntPtr.Zero;
        }

        // 调用原始窗口过程
        return CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
    }

    private void StartMonitoring()
    {
        // 启动监控定时器，检测CMD窗口状态
        _monitorTimer = new System.Windows.Forms.Timer();
        _monitorTimer.Interval = 500;
        _monitorTimer.Tick += (s, e) =>
        {
            if (_cmdHwnd != IntPtr.Zero && _cmdProcess != null && !_cmdProcess.HasExited)
            {
                // 检查窗口是否可见
                // 如果窗口被用户关闭，我们可以在这里检测到
            }
        };
        _monitorTimer.Start();
    }

    public void ShowCmdWindow()
    {
        // 如果CMD进程已退出，重新启动
        if (_cmdProcess == null || _cmdProcess.HasExited)
        {
            StartCmdProcess();
        }
        else if (_cmdHwnd != IntPtr.Zero)
        {
            ShowWindow(_cmdHwnd, SW_RESTORE);
            SetForegroundWindow(_cmdHwnd);
        }
    }

    public void HideCmdWindow()
    {
        if (_cmdHwnd != IntPtr.Zero && _cmdProcess != null && !_cmdProcess.HasExited)
        {
            ShowWindow(_cmdHwnd, SW_HIDE);
        }
    }

    public bool IsCmdRunning()
    {
        return _cmdProcess != null && !_cmdProcess.HasExited;
    }

    public bool IsCmdVisible()
    {
        if (_cmdHwnd != IntPtr.Zero && _cmdProcess != null && !_cmdProcess.HasExited)
        {
            return IsWindowVisible(_cmdHwnd);
        }
        return false;
    }

    private void UpdateStatusFile(string statusFile)
    {
        try
        {
            var content = $"Name: {_robot.Name}\n" +
                          $"ID: {_robot.Id}\n" +
                          $"Status: {(_robot.IsMoving ? "MOVING" : "STOPPED")}\n" +
                          $"Speed: {_robot.SpeedMultiplier:F1}x\n" +
                          $"Size: {_robot.Size}px\n" +
                          $"Position: ({_robot.X:F0}, {_robot.Y:F0})\n" +
                          $"Facing: {(_robot.FacingRight ? "RIGHT" : "LEFT")}\n";
            System.IO.File.WriteAllText(statusFile, content);
        }
        catch { }
    }

    private void TerminalForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        // 如果不是允许关闭，则取消
        if (!_allowClose && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            return;
        }

        // 真正关闭时的清理
        _monitorTimer?.Stop();
        _monitorTimer?.Dispose();
        _trayIcon?.Dispose();

        // 恢复机器人移动
        _robot.IsMoving = true;

        // 取消Hook
        UnhookCmdWindow();

        // 关闭 CMD 进程
        try
        {
            if (_cmdProcess != null && !_cmdProcess.HasExited)
            {
                _cmdProcess.Kill();
            }
        }
        catch { }

        _robot.Terminal = null;
    }
}
