using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CockroachPet;

public class TerminalManagerForm : Form
{
    private TabControl _tabControl;
    private Dictionary<Robot, TerminalTab> _terminals = new Dictionary<Robot, TerminalTab>();
    private static TerminalManagerForm? _instance;

    public static TerminalManagerForm Instance
    {
        get
        {
            if (_instance == null || _instance.IsDisposed)
            {
                _instance = new TerminalManagerForm();
            }
            return _instance;
        }
    }

    private TerminalManagerForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "🤖 Robot Terminal Manager";
        this.Size = new Size(1000, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.MinimumSize = new Size(600, 400);

        // 标签页控件
        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.White,
            Font = new Font("Consolas", 10),
            Padding = new Point(15, 5)
        };

        this.Controls.Add(_tabControl);
        this.FormClosing += TerminalManagerForm_FormClosing;
    }

    public void OpenTerminal(Robot robot)
    {
        if (_terminals.ContainsKey(robot))
        {
            // 终端已存在，切换到该标签页
            var tab = _terminals[robot];
            _tabControl.SelectedTab = tab.TabPage;
            this.Show();
            this.Activate();
            tab.FocusTerminal();
        }
        else
        {
            // 创建新的终端标签页
            var tab = new TerminalTab(robot);
            _terminals[robot] = tab;
            _tabControl.TabPages.Add(tab.TabPage);
            _tabControl.SelectedTab = tab.TabPage;
            
            this.Show();
            this.Activate();
            
            // 确保窗口和标签页完全显示后再启动终端
            Application.DoEvents();
            
            // 使用 BeginInvoke 在消息队列处理完后执行
            this.BeginInvoke(new Action(() =>
            {
                tab.StartTerminal();
            }));
        }
    }

    public void CloseTerminal(Robot robot)
    {
        if (_terminals.ContainsKey(robot))
        {
            var tab = _terminals[robot];
            tab.Dispose();
            _tabControl.TabPages.Remove(tab.TabPage);
            _terminals.Remove(robot);

            if (_terminals.Count == 0)
            {
                this.Hide();
            }
        }
    }

    private void TerminalManagerForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            this.Hide();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var tab in _terminals.Values)
            {
                tab.Dispose();
            }
            _terminals.Clear();
        }
        base.Dispose(disposing);
    }
}

public class TerminalTab : IDisposable
{
    private Robot _robot;
    private TabPage _tabPage;
    private Panel _terminalPanel;
    private Process? _cmdProcess;
    private IntPtr _cmdHwnd = IntPtr.Zero;

    // Win32 API
    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

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

    public TabPage TabPage => _tabPage;

    public TerminalTab(Robot robot)
    {
        _robot = robot;
        InitializeTab();
        // 不要在构造函数中启动 CMD，等标签页添加到 TabControl 后再启动
    }

    public void StartTerminal()
    {
        // 在标签页添加到 TabControl 后调用此方法
        StartCmdProcess();
    }

    private void InitializeTab()
    {
        _tabPage = new TabPage
        {
            Text = $"  {_robot.Name}  ",
            BackColor = Color.Black,
            Padding = new Padding(0)
        };

        _terminalPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black,
            BorderStyle = BorderStyle.None
        };

        _terminalPanel.Click += (s, e) => FocusTerminal();
        _terminalPanel.Resize += (s, e) => ResizeTerminal();
        
        // 确保 Panel 句柄被创建
        if (!_terminalPanel.IsHandleCreated)
        {
            var handle = _terminalPanel.Handle; // 强制创建句柄
        }

        _tabPage.Controls.Add(_terminalPanel);
    }

    private void StartCmdProcess()
    {
        try
        {
            // 确保 Panel 句柄已创建
            if (!_terminalPanel.IsHandleCreated)
            {
                var handle = _terminalPanel.Handle;
            }
            
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RobotTerminals");
            System.IO.Directory.CreateDirectory(tempDir);
            string batchFile = System.IO.Path.Combine(tempDir, $"robot_{_robot.Id}.bat");
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
                "echo   cls           - Clear screen\n" +
                "echo.\n" +
                "echo ==========================================\n" +
                "echo.\n" +
                "doskey robot-name=echo Name: " + _robot.Name + " ^& echo ID: " + _robot.Id + "\n" +
                "doskey robot-status=echo Status: " + (_robot.IsMoving ? "MOVING" : "STOPPED") + "\n" +
                "\n" +
                $"prompt [{_robot.Name}]$G \n" +
                "cmd /k\n";
            System.IO.File.WriteAllText(batchFile, batchContent);

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

            // 等待窗口创建，然后嵌入 - 增加等待时间到 800ms
            System.Threading.Tasks.Task.Delay(800).ContinueWith(_ =>
            {
                if (_terminalPanel != null && _terminalPanel.IsHandleCreated && !_terminalPanel.IsDisposed)
                {
                    try
                    {
                        _terminalPanel.Invoke(new Action(() =>
                        {
                            FindAndEmbedCmdWindow(windowTitle);
                        }));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{_robot.Name}] Error invoking embed: {ex.Message}");
                    }
                }
            });

            _cmdProcess.Exited += (s, e) =>
            {
                _cmdHwnd = IntPtr.Zero;
                _cmdProcess = null;
            };
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start terminal: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void FindAndEmbedCmdWindow(string windowTitle)
    {
        // 查找窗口
        for (int i = 0; i < 30 && _cmdHwnd == IntPtr.Zero; i++)
        {
            System.Threading.Thread.Sleep(50);
            _cmdHwnd = FindWindow(null, windowTitle);
        }

        if (_cmdHwnd == IntPtr.Zero && _cmdProcess != null)
        {
            _cmdProcess.Refresh();
            _cmdHwnd = _cmdProcess.MainWindowHandle;
        }

        if (_cmdHwnd != IntPtr.Zero)
        {
            // 直接嵌入，不要先隐藏
            EmbedCmdWindow();
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[{_robot.Name}] Failed to find CMD window: {windowTitle}");
        }
    }

    private void EmbedCmdWindow()
    {
        if (_cmdHwnd == IntPtr.Zero || _terminalPanel == null) return;

        try
        {
            System.Diagnostics.Debug.WriteLine($"[{_robot.Name}] Embedding CMD window: {_cmdHwnd}");
            
            // 将CMD窗口设置为子窗口
            SetParent(_cmdHwnd, _terminalPanel.Handle);

            // 移除边框和标题栏
            int style = GetWindowLong(_cmdHwnd, GWL_STYLE);
            style &= ~WS_CAPTION;
            style &= ~WS_BORDER;
            style |= WS_CHILD;
            SetWindowLong(_cmdHwnd, GWL_STYLE, style);

            // 调整大小以填充面板
            MoveWindow(_cmdHwnd, 0, 0, _terminalPanel.Width, _terminalPanel.Height, true);
            
            // 显示窗口
            ShowWindow(_cmdHwnd, SW_SHOW);

            // 连接输入队列
            AttachInputThreads();
            
            // 设置焦点
            SetFocus(_cmdHwnd);
            
            System.Diagnostics.Debug.WriteLine($"[{_robot.Name}] CMD window embedded successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{_robot.Name}] Failed to embed CMD: {ex.Message}");
        }
    }

    private void AttachInputThreads()
    {
        try
        {
            uint currentThreadId = GetCurrentThreadId();
            uint cmdThreadId = GetWindowThreadProcessId(_cmdHwnd, out _);

            if (currentThreadId != cmdThreadId)
            {
                AttachThreadInput(cmdThreadId, currentThreadId, true);
            }
        }
        catch { }
    }

    public void FocusTerminal()
    {
        if (_cmdHwnd != IntPtr.Zero)
        {
            AttachInputThreads();
            SetFocus(_cmdHwnd);
        }
    }

    private void ResizeTerminal()
    {
        if (_cmdHwnd != IntPtr.Zero && _terminalPanel != null)
        {
            MoveWindow(_cmdHwnd, 0, 0, _terminalPanel.Width, _terminalPanel.Height, true);
        }
    }

    public void Dispose()
    {
        try
        {
            if (_cmdProcess != null && !_cmdProcess.HasExited)
            {
                _cmdProcess.Kill();
            }
        }
        catch { }

        _cmdProcess?.Dispose();
    }
}
