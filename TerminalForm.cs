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
    private Panel _terminalPanel;
    private Label _titleLabel;
    private Button _closeButton;

    // Win32 API
    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetFocus(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int GWL_STYLE = -16;
    private const int WS_BORDER = 0x00800000;
    private const int WS_CAPTION = 0x00C00000;
    private const int WS_CHILD = 0x40000000;
    private const int WS_VISIBLE = 0x10000000;

    public TerminalForm(Robot robot)
    {
        _robot = robot;
        InitializeComponent();
        StartCmdProcess();
    }

    private void InitializeComponent()
    {
        // 窗口设置
        this.Text = $"🤖 {_robot.Name} - Terminal";
        this.Size = new Size(800, 600);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.MinimumSize = new Size(400, 300);

        // 标题栏
        _titleLabel = new Label
        {
            Text = $"🤖 {_robot.Name} - Integrated Terminal",
            Dock = DockStyle.Top,
            Height = 40,
            Font = new Font("Microsoft YaHei", 12, FontStyle.Bold),
            ForeColor = Color.Lime,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.FromArgb(40, 40, 40),
            Padding = new Padding(10, 0, 0, 0)
        };

        // 关闭按钮
        _closeButton = new Button
        {
            Text = "✕",
            Size = new Size(40, 40),
            Location = new Point(this.ClientSize.Width - 40, 0),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.White,
            Font = new Font("Arial", 14, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _closeButton.FlatAppearance.BorderSize = 0;
        _closeButton.Click += (s, e) => this.Hide();
        _closeButton.MouseEnter += (s, e) => _closeButton.BackColor = Color.Red;
        _closeButton.MouseLeave += (s, e) => _closeButton.BackColor = Color.FromArgb(40, 40, 40);

        // 终端容器面板
        _terminalPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black,
            BorderStyle = BorderStyle.None
        };
        
        // 点击面板时设置焦点到 CMD 窗口
        _terminalPanel.Click += (s, e) =>
        {
            if (_cmdHwnd != IntPtr.Zero)
            {
                AttachInputThreads();
                SetFocus(_cmdHwnd);
            }
        };

        this.Controls.Add(_terminalPanel);
        this.Controls.Add(_titleLabel);
        this.Controls.Add(_closeButton);

        // 窗口事件
        this.FormClosing += TerminalForm_FormClosing;
        this.Resize += TerminalForm_Resize;
        this.Shown += TerminalForm_Shown;
        this.Activated += TerminalForm_Activated;
    }

    private void TerminalForm_Activated(object? sender, EventArgs e)
    {
        // 窗口激活时，重新连接输入队列并设置焦点
        if (_cmdHwnd != IntPtr.Zero)
        {
            AttachInputThreads();
            SetFocus(_cmdHwnd);
        }
    }

    private void TerminalForm_Shown(object? sender, EventArgs e)
    {
        // 窗口显示后，嵌入CMD窗口
        if (_cmdHwnd != IntPtr.Zero)
        {
            EmbedCmdWindow();
            // 设置焦点到 CMD 窗口
            SetFocus(_cmdHwnd);
        }
    }

    private void TerminalForm_Resize(object? sender, EventArgs e)
    {
        // 窗口大小改变时，调整CMD窗口大小
        if (_cmdHwnd != IntPtr.Zero && _terminalPanel != null)
        {
            MoveWindow(_cmdHwnd, 0, 0, _terminalPanel.Width, _terminalPanel.Height, true);
        }
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
                "echo.\n" +
                "echo Type 'exit' to close terminal\n" +
                "echo ==========================================\n" +
                "echo.\n" +
                "doskey robot-name=echo Name: " + _robot.Name + " ^& echo ID: " + _robot.Id + "\n" +
                "doskey robot-status=type \"" + statusFile + "\" 2^>nul ^|^| echo Status file not found\n" +
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
            System.Threading.Tasks.Task.Delay(500).ContinueWith(_ =>
            {
                this.Invoke(new Action(() =>
                {
                    FindCmdWindow(windowTitle);
                    if (_cmdHwnd != IntPtr.Zero && this.Visible)
                    {
                        EmbedCmdWindow();
                    }
                }));
            });

            _cmdProcess.Exited += (s, e) =>
            {
                this.Invoke(new Action(() =>
                {
                    _cmdHwnd = IntPtr.Zero;
                    _cmdProcess = null;
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

    private void EmbedCmdWindow()
    {
        if (_cmdHwnd == IntPtr.Zero || _terminalPanel == null) return;

        try
        {
            // 将CMD窗口设置为子窗口
            SetParent(_cmdHwnd, _terminalPanel.Handle);

            // 移除边框和标题栏
            int style = GetWindowLong(_cmdHwnd, GWL_STYLE);
            style &= ~WS_CAPTION;  // 移除标题栏
            style &= ~WS_BORDER;   // 移除边框
            style |= WS_CHILD;     // 设置为子窗口
            SetWindowLong(_cmdHwnd, GWL_STYLE, style);

            // 调整大小以填充面板
            MoveWindow(_cmdHwnd, 0, 0, _terminalPanel.Width, _terminalPanel.Height, true);

            // 显示窗口
            ShowWindow(_cmdHwnd, SW_SHOW);
            
            // 关键：连接输入队列以支持键盘输入
            AttachInputThreads();
            
            // 设置焦点到 CMD 窗口
            SetFocus(_cmdHwnd);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to embed CMD window: {ex.Message}");
        }
    }

    private void AttachInputThreads()
    {
        try
        {
            // 获取当前线程 ID
            uint currentThreadId = GetCurrentThreadId();
            
            // 获取 CMD 窗口的线程 ID
            uint cmdThreadId = GetWindowThreadProcessId(_cmdHwnd, out _);
            
            if (currentThreadId != cmdThreadId)
            {
                // 连接两个线程的输入队列
                AttachThreadInput(cmdThreadId, currentThreadId, true);
                System.Diagnostics.Debug.WriteLine($"Attached input threads: CMD={cmdThreadId}, Current={currentThreadId}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to attach input threads: {ex.Message}");
        }
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

    public void ShowTerminal()
    {
        if (!this.Visible)
        {
            this.Show();
            if (_cmdHwnd != IntPtr.Zero)
            {
                ShowWindow(_cmdHwnd, SW_SHOW);
                AttachInputThreads();
                SetFocus(_cmdHwnd);
            }
        }
        else
        {
            this.Activate();
            if (_cmdHwnd != IntPtr.Zero)
            {
                AttachInputThreads();
                SetFocus(_cmdHwnd);
            }
        }
    }

    public void HideTerminal()
    {
        this.Hide();
    }

    public bool IsCmdRunning()
    {
        return _cmdProcess != null && !_cmdProcess.HasExited;
    }

    public bool IsCmdVisible()
    {
        return this.Visible;
    }

    // 兼容旧代码的方法
    public void ShowCmdWindow()
    {
        ShowTerminal();
    }

    public void HideCmdWindow()
    {
        HideTerminal();
    }

    private void TerminalForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        // 阻止关闭，改为隐藏
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            this.Hide();
            return;
        }

        // 真正关闭时的清理
        _robot.IsMoving = true;

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
