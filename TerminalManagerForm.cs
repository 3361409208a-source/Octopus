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
    private RichTextBox _outputBox;
    private TextBox _inputBox;
    private Process? _cmdProcess;
    private List<string> _commandHistory = new List<string>();
    private int _historyIndex = -1;

    public TabPage TabPage => _tabPage;

    public TerminalTab(Robot robot)
    {
        _robot = robot;
        InitializeTab();
    }

    private void InitializeTab()
    {
        _tabPage = new TabPage
        {
            Text = $"  {_robot.Name}  ",
            BackColor = Color.Black,
            Padding = new Padding(3)
        };

        // 容器面板
        Panel container = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black };
        
        // 输入框 (底部)
        _inputBox = new TextBox
        {
            Dock = DockStyle.Bottom,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.FromArgb(0, 255, 0),
            Font = new Font("Consolas", 11),
            BorderStyle = BorderStyle.FixedSingle
        };
        _inputBox.KeyDown += InputBox_KeyDown;

        // 输出框 (填充)
        _outputBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black,
            ForeColor = Color.FromArgb(0, 200, 0),
            Font = new Font("Consolas", 11),
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            ScrollBars = RichTextBoxScrollBars.Vertical
        };

        container.Controls.Add(_outputBox);
        container.Controls.Add(_inputBox);
        _tabPage.Controls.Add(container);
    }

    public void StartTerminal()
    {
        try
        {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RobotTerminals");
            System.IO.Directory.CreateDirectory(tempDir);
            string batchFile = System.IO.Path.Combine(tempDir, $"robot_{_robot.Id}.bat");

            string batchContent = "@echo off\n" +
                "chcp 65001 >nul 2>&1\n" +
                "cls\n" +
                "echo ==========================================\n" +
                $"echo  Robot: {_robot.Name}    ID: {_robot.Id:D3}\n" +
                "echo ==========================================\n" +
                "echo.\n" +
                "echo Robot Commands:\n" +
                "echo   robot-name    - Show robot name\n" +
                "echo   robot-status  - Show robot status\n" +
                "echo.\n" +
                "echo [Capturing Output for Robot AI Interaction]\n" +
                "echo ==========================================\n" +
                "echo.\n" +
                "doskey robot-name=echo Name: " + _robot.Name + " ^& echo ID: " + _robot.Id + "\n" +
                "doskey robot-status=echo Status: " + _robot.StatusMessage + "\n" +
                "\n" +
                "cmd /k\n";
            System.IO.File.WriteAllText(batchFile, batchContent);

            _cmdProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{batchFile}\"",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                },
                EnableRaisingEvents = true
            };

            _cmdProcess.OutputDataReceived += (s, e) => AppendText(e.Data);
            _cmdProcess.ErrorDataReceived += (s, e) => AppendText(e.Data, Color.Red);

            _cmdProcess.Start();
            _cmdProcess.BeginOutputReadLine();
            _cmdProcess.BeginErrorReadLine();

            FocusTerminal();
        }
        catch (Exception ex)
        {
            AppendText("ERROR: Failed to start CMD process: " + ex.Message, Color.Red);
        }
    }

    private void AppendText(string? text, Color? color = null)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (_tabPage.InvokeRequired)
        {
            _tabPage.BeginInvoke(new Action(() => AppendText(text, color)));
            return;
        }

        _outputBox.SelectionStart = _outputBox.TextLength;
        _outputBox.SelectionLength = 0;
        _outputBox.SelectionColor = color ?? Color.FromArgb(0, 200, 0);
        _outputBox.AppendText(text + Environment.NewLine);
        _outputBox.ScrollToCaret();

        // 同时通知机器人：如果是红色（错误流），强制触发警告
        _robot.NotifyOutput(text, color == Color.Red);
    }

    private void InputBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            string cmd = _inputBox.Text;
            if (!string.IsNullOrWhiteSpace(cmd))
            {
                _commandHistory.Add(cmd);
                _historyIndex = _commandHistory.Count;
                
                if (_cmdProcess != null && !_cmdProcess.HasExited)
                {
                    _cmdProcess.StandardInput.WriteLine(cmd);
                }
                _inputBox.Clear();
            }
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Up)
        {
            if (_historyIndex > 0)
            {
                _historyIndex--;
                _inputBox.Text = _commandHistory[_historyIndex];
                _inputBox.SelectionStart = _inputBox.Text.Length;
            }
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Down)
        {
            if (_historyIndex < _commandHistory.Count - 1)
            {
                _historyIndex++;
                _inputBox.Text = _commandHistory[_historyIndex];
                _inputBox.SelectionStart = _inputBox.Text.Length;
            }
            else
            {
                _historyIndex = _commandHistory.Count;
                _inputBox.Clear();
            }
            e.Handled = true;
        }
    }

    public void FocusTerminal()
    {
        _inputBox.Focus();
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
