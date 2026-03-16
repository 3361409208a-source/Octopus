using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CockroachPet;

public partial class Form1 : Form
{
    // Windows API for global hotkeys
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int HOTKEY_ID_MENU = 1;
    private const int HOTKEY_ID_TOGGLE = 2;
    private const int HOTKEY_ID_PAUSE = 3;
    private const uint MOD_CTRL_SHIFT = 0x0002 | 0x0004; // MOD_CONTROL | MOD_SHIFT

    // 机器人列表
    private List<Robot> _robots = new List<Robot>();

    // 定时器
    private System.Windows.Forms.Timer? _moveTimer;

    // 全局速度
    private int _globalSpeed = 100;

    // 机器人ID计数器
    private int _robotIdCounter = 1;

    // 通知图标
    private NotifyIcon? _notifyIcon;

    // 点击穿透
    private bool _clickThrough = true;

    // 默认设置
    public int DefaultRobotSize { get; set; } = 64;
    public string DefaultRobotName { get; set; } = "小八";
    public int DefaultRobotCount { get; set; } = 1;
    public bool ShowNamingDialog { get; set; } = false; // 默认不显示命名对话框

    // 设置窗口单例
    private SettingsForm? _settingsForm = null;

    // 控制面板单例
    private ControlPanelForm? _controlPanel = null;

    public Form1()
    {
        InitializeComponent();
        InitializeWindow();
        LoadSettingsAndStart();
    }

    private void LoadSettingsAndStart()
    {
        // 尝试从文件加载设置
        LoadSettingsFromFile();

        // 初始化托盘图标
        InitNotifyIcon();

        // 加载持久化机器人
        var savedRobots = PersistenceManager.LoadRobots();
        if (savedRobots.Count > 0)
        {
            foreach (var data in savedRobots)
            {
                RestoreRobot(data);
                if (data.Id >= _robotIdCounter) _robotIdCounter = data.Id + 1;
            }
            ShowNotification($"成功找回 {savedRobots.Count} 个伙伴！");
        }
        else
        {
            // 默认投放
            if (ShowNamingDialog)
            {
                SpawnRobotsWithNaming(DefaultRobotCount);
            }
            else
            {
                for (int i = 0; i < DefaultRobotCount; i++)
                {
                    string name = DefaultRobotCount == 1
                        ? DefaultRobotName
                        : $"{DefaultRobotName}-{i + 1}";
                    SpawnRobot(name, -1, -1);
                }
            }
        }
        
        // 自动打开所有终端
        AutoOpenAllTerminals();
    }

    private void AutoOpenAllTerminals()
    {
        if (_robots.Count > 0)
        {
            var manager = TerminalManagerForm.Instance;
            manager.Show();
            foreach (var robot in _robots)
            {
                manager.OpenTerminal(robot);
            }
        }
    }

    private Icon CreateRobotIcon()
    {
        // 创建像素八爪鱼图标
        var iconBitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(iconBitmap))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            // 绘制简单的像素八爪鱼
            using var bodyBrush = new SolidBrush(Color.FromArgb(77, 171, 255)); // 蓝色
            using var tentacleBrush = new SolidBrush(Color.FromArgb(51, 153, 255));
            using var eyeBrush = new SolidBrush(Color.White);
            using var pupilBrush = new SolidBrush(Color.Black);

            // 身体 (圆形)
            g.FillEllipse(bodyBrush, 8, 6, 16, 14);

            // 触手 (8条)
            for (int i = 0; i < 8; i++)
            {
                float angle = i * (float)(Math.PI / 4);
                int tx = 16 + (int)(Math.Cos(angle) * 10);
                int ty = 13 + (int)(Math.Sin(angle) * 8);
                g.FillRectangle(tentacleBrush, tx - 1, ty - 1, 3, 3);
            }

            // 眼睛
            g.FillEllipse(eyeBrush, 11, 8, 5, 5);
            g.FillEllipse(eyeBrush, 18, 8, 5, 5);
            g.FillRectangle(pupilBrush, 13, 9, 2, 2);
            g.FillRectangle(pupilBrush, 20, 9, 2, 2);
        }
        return Icon.FromHandle(iconBitmap.GetHicon());
    }

    private void LoadSettingsFromFile()
    {
        try
        {
            string settingsPath = Path.Combine(Path.GetTempPath(), "RobotPetSettings.txt");
            if (File.Exists(settingsPath))
            {
                var lines = File.ReadAllLines(settingsPath);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        switch (parts[0])
                        {
                            case "Count": DefaultRobotCount = int.Parse(parts[1]); break;
                            case "ShowNaming": ShowNamingDialog = bool.Parse(parts[1]); break;
                            case "DefaultName": DefaultRobotName = parts[1]; break;
                            case "DefaultSize": DefaultRobotSize = int.Parse(parts[1]); break;
                            case "DefaultSpeed": _globalSpeed = int.Parse(parts[1]); break;
                        }
                    }
                }
            }
            // 文件不存在就用默认值
        }
        catch
        {
            // 出错用默认值
        }
    }

    private void InitializeWindow()
    {
        int screenWidth = Screen.PrimaryScreen!.Bounds.Width;
        int screenHeight = Screen.PrimaryScreen.Bounds.Height;

        // 全屏无边框
        this.Text = "Pixel Robot Pet";
        this.StartPosition = FormStartPosition.Manual;
        this.Location = Point.Empty;
        this.Size = new Size(screenWidth, screenHeight);
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Maximized;
        this.TopMost = true;
        this.ShowInTaskbar = false; // 主窗口不显示在任务栏

        // 透明设置
        this.BackColor = Color.Black;
        this.TransparencyKey = Color.Black;
        this.AllowTransparency = true;
        this.DoubleBuffered = true;

        // 启用点击穿透
        SetClickThrough(true);

        // 设置窗口图标
        this.Icon = CreateRobotIcon();

        // 定时器
        _moveTimer = new System.Windows.Forms.Timer();
        _moveTimer.Interval = 30;
        _moveTimer.Tick += MoveTimer_Tick;
        _moveTimer.Start();

        // 事件绑定
        this.Paint += Form1_Paint;
        this.MouseClick += Form1_MouseClick;
        this.KeyDown += Form1_KeyDown;

        // 注册全局热键
        RegisterGlobalHotkeys();
    }

    private void RegisterGlobalHotkeys()
    {
        // 使用组合键避免干扰正常操作
        // Ctrl+Shift+P = 暂停/继续所有
        RegisterHotKey(this.Handle, HOTKEY_ID_PAUSE, MOD_CTRL_SHIFT, 0x50); // P
        // Ctrl+Shift+T = 切换点击穿透
        RegisterHotKey(this.Handle, HOTKEY_ID_TOGGLE, MOD_CTRL_SHIFT, 0x54); // T
        // Ctrl+Shift+M = 打开菜单
        RegisterHotKey(this.Handle, HOTKEY_ID_MENU, MOD_CTRL_SHIFT, 0x4D); // M
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_HOTKEY = 0x0312;
        if (m.Msg == WM_HOTKEY)
        {
            int id = m.WParam.ToInt32();
            switch (id)
            {
                case HOTKEY_ID_MENU:
                    ShowTrayMenu();
                    break;
                case HOTKEY_ID_TOGGLE:
                    SetClickThrough(!_clickThrough);
                    ShowNotification(_clickThrough ? "点击穿透: 开启" : "点击穿透: 关闭");
                    break;
                case HOTKEY_ID_PAUSE:
                    TogglePauseAll();
                    break;
            }
        }
        base.WndProc(ref m);
    }

    private void ShowTrayMenu()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.ContextMenuStrip = CreateContextMenu();
            var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            mi?.Invoke(_notifyIcon, null);
        }
    }

    private void TogglePauseAll()
    {
        bool anyMoving = _robots.Any(r => r.IsMoving);
        foreach (var r in _robots) r.IsMoving = !anyMoving;
        ShowNotification(anyMoving ? "全部暂停" : "全部继续");
    }

    private void SpawnRobotsWithNaming(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnRobotWithName();
        }
    }

    private void ShowNotification(string message)
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.BalloonTipTitle = "Pixel Robot";
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.ShowBalloonTip(2000);
        }
    }

    private void InitNotifyIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Text = "Pixel Robot Pet",
            Icon = CreateRobotIcon(),
            Visible = true
        };

        // 直接绑定右键菜单
        _notifyIcon.ContextMenuStrip = CreateContextMenu();

        // 左键点击也显示菜单
        _notifyIcon.MouseClick += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                // 刷新菜单内容
                _notifyIcon.ContextMenuStrip = CreateContextMenu();
                // 显示菜单
                var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                mi?.Invoke(_notifyIcon, null);
            }
        };

        // 启动提示
        _notifyIcon.BalloonTipTitle = "Pixel Robot Pet";
        _notifyIcon.BalloonTipText = "系统已启动！\nCtrl+Shift+M 打开菜单\nCtrl+Shift+P 暂停/继续";
        _notifyIcon.ShowBalloonTip(3000);
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();

        // 机器人列表
        if (_robots.Count > 0)
        {
            menu.Items.Add("机器人列表:").Enabled = false;
            foreach (var robot in _robots)
            {
                var robotMenu = new ToolStripMenuItem($"{robot.Name} (#{robot.Id})");
                
                // 终端菜单
                robotMenu.DropDownItems.Add("📺 打开终端", null, (s, e) => robot.OpenTerminal());
                robotMenu.DropDownItems.Add("🗕 关闭终端", null, (s, e) => robot.CloseTerminal());
                
                robotMenu.DropDownItems.Add(new ToolStripSeparator());
                
                // 机器人控制
                var status = robot.IsMoving ? "⏸ 暂停移动" : "▶ 恢复移动";
                robotMenu.DropDownItems.Add(status, null, (s, e) => robot.IsMoving = !robot.IsMoving);
                
                menu.Items.Add(robotMenu);
            }
            menu.Items.Add(new ToolStripSeparator());
        }

        // 新增机器人
        menu.Items.Add("➕ 投放新机器人", null, (s, e) => SpawnRobotWithName());
        menu.Items.Add("⚡ 快速投放", null, (s, e) =>
        {
            string[] names = { "小八", "阿呆", "像素仔", "蓝灵", "红豆", "大眼", "触手大王", "碳基生物" };
            SpawnRobot(names[new Random().Next(names.Length)], -1, -1);
        });

        menu.Items.Add(new ToolStripSeparator());

        // 控制面板
        menu.Items.Add("🎛️ 打开控制面板", null, (s, e) => ShowControlPanel());

        menu.Items.Add(new ToolStripSeparator());

        // 全局控制
        var controlMenu = new ToolStripMenuItem("全局控制");
        controlMenu.DropDownItems.Add("全部暂停", null, (s, e) =>
        {
            foreach (var r in _robots) r.IsMoving = false;
        });
        controlMenu.DropDownItems.Add("全部启动", null, (s, e) =>
        {
            foreach (var r in _robots) r.IsMoving = true;
        });
        controlMenu.DropDownItems.Add(new ToolStripSeparator());
        controlMenu.DropDownItems.Add("全部清除", null, (s, e) =>
        {
            _robots.Clear();
        });
        menu.Items.Add(controlMenu);

        // 速度控制
        var speedMenu = new ToolStripMenuItem("全局速度");
        var slowItem = new ToolStripMenuItem("慢速 (50%)");
        slowItem.Click += (s, e) => SetGlobalSpeed(50);
        var normalItem = new ToolStripMenuItem("正常 (100%)");
        normalItem.Click += (s, e) => SetGlobalSpeed(100);
        var fastItem = new ToolStripMenuItem("快速 (200%)");
        fastItem.Click += (s, e) => SetGlobalSpeed(200);
        speedMenu.DropDownItems.AddRange(new[] { slowItem, normalItem, fastItem });
        menu.Items.Add(speedMenu);

        menu.Items.Add(new ToolStripSeparator());

        // 设置 - 单例模式
        menu.Items.Add("⚙️ 设置...", null, (s, e) => ShowSettings());

        // 快捷键提示
        menu.Items.Add("ℹ️ 快捷键", null, (s, e) => ShowShortcuts());

        // 关于
        menu.Items.Add("❓ 关于", null, (s, e) =>
        {
            MessageBox.Show(
                "Pixel Robot Pet\n\n" +
                "桌面八爪鱼机器人宠物\n" +
                "点击机器人打开CMD终端\n\n" +
                "Version 2.0",
                "关于",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        });

        // 退出
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("❌ 退出程序", null, (s, e) => ExitApplication());

        return menu;
    }

    private void ShowShortcuts()
    {
        using var dialog = new Form
        {
            Text = "快捷键",
            Size = new Size(480, 450),
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.White
        };

        var text = @"像素八爪鱼机器人 - 快捷键

【全局快捷键（任何窗口都可用）】
  Ctrl+Shift+P       - 暂停/继续所有机器人
  Ctrl+Shift+T       - 切换点击穿透模式
  Ctrl+Shift+M       - 打开菜单

【程序内快捷键（关闭点击穿透后可用）】
  ESC                - 打开菜单
  F11                - 切换点击穿透模式
  空格               - 暂停/继续所有机器人

鼠标:
  左键点击机器人      - 打开该机器人的CMD终端
  右键托盘图标        - 打开菜单

终端操作:
  ESC                - 隐藏终端到托盘
  点击 X 按钮        - 隐藏到后台（机器人继续移动）
  CMD中输入 exit     - 真正关闭终端

机器人命令:
  robot-name         - 显示名字和ID
  robot-status       - 显示状态
  robot-resume       - 恢复移动
  robot-stop         - 停止移动
  robot-help         - 显示帮助

托盘图标操作:
  左键/右键点击      - 显示菜单
  双击终端托盘图标   - 恢复终端窗口";

        var textBox = new TextBox
        {
            Text = text,
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            BackColor = Color.Black,
            ForeColor = Color.Lime,
            BorderStyle = BorderStyle.None,
            ScrollBars = ScrollBars.Vertical,
            Padding = new Padding(10)
        };

        dialog.Controls.Add(textBox);
        dialog.ShowDialog();
    }

    private void SetGlobalSpeed(int speed)
    {
        _globalSpeed = speed;
        foreach (var r in _robots)
        {
            r.SpeedMultiplier = speed / 100f;
        }
    }

    private void OpenRobotTerminal(Robot robot)
    {
        robot.OpenTerminal();
    }

    private void SetClickThrough(bool enable)
    {
        _clickThrough = enable;
        this.Enabled = !enable;
    }

    private void MoveTimer_Tick(object? sender, EventArgs e)
    {
        int screenWidth = Screen.PrimaryScreen!.Bounds.Width;
        int screenHeight = Screen.PrimaryScreen.Bounds.Height;

        for (int i = 0; i < _robots.Count; i++)
        {
            var robot = _robots[i];
            if (!robot.IsVisible) continue;
            
            robot.Update(screenWidth, screenHeight);

            // 检查与其他机器人的互动
            for (int j = i + 1; j < _robots.Count; j++)
            {
                if (_robots[j].IsVisible)
                    robot.InteractWith(_robots[j]);
            }
        }
        this.Invalidate();
    }

    private void Form1_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

        foreach (var robot in _robots)
        {
            if (robot.IsVisible)
                PixelRobotRenderer.DrawRobot(e.Graphics, robot);
        }
    }

    private void Form1_MouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;

        SetClickThrough(false);

        // 从后向前检测
        for (int i = _robots.Count - 1; i >= 0; i--)
        {
            var robot = _robots[i];
            if (robot.HitTest(e.X, e.Y))
            {
                robot.OpenTerminal();
                return;
            }
        }

        SetClickThrough(true);
    }

    private void Form1_KeyDown(object? sender, KeyEventArgs e)
    {
        // 这些快捷键只在程序有焦点时响应（点击穿透关闭时）
        if (e.KeyCode == Keys.F11)
        {
            SetClickThrough(!_clickThrough);
        }
        else if (e.KeyCode == Keys.Escape)
        {
            // 显示菜单
            if (_notifyIcon != null)
            {
                _notifyIcon.ContextMenuStrip = CreateContextMenu();
                _notifyIcon.ContextMenuStrip.Show(Cursor.Position);
            }
        }
        else if (e.KeyCode == Keys.Space)
        {
            TogglePauseAll();
        }
    }

    private void ExitApplication()
    {
        PersistenceManager.SaveRobots(_robots);
        _notifyIcon?.Dispose();
        _controlPanel?.Close();
        _settingsForm?.Close();
        Application.Exit();
    }

    // 公共方法供控制面板使用
    public List<Robot> GetRobots() => _robots;

    public void SpawnRobotWithName()
    {
        using var nameDialog = new Form
        {
            Text = "命名机器人",
            Size = new Size(350, 180),
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.FromArgb(40, 40, 40)
        };

        var names = new[] { "Claude", "Alpha", "Beta", "Gamma", "Delta", "Octo", "Pixel", "Byte", "Bit", "Neo" };
        var defaultName = _robotIdCounter == 1 ? DefaultRobotName : names[new Random().Next(names.Length)];

        var label = new Label
        {
            Text = $"为机器人 #{_robotIdCounter} 命名:",
            Location = new Point(20, 20),
            Size = new Size(300, 30),
            Font = new Font("Microsoft YaHei", 11),
            ForeColor = Color.White
        };

        var textBox = new TextBox
        {
            Location = new Point(20, 55),
            Size = new Size(290, 30),
            Text = defaultName,
            Font = new Font("Microsoft YaHei", 11),
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var btnOk = new Button
        {
            Text = "投放",
            Location = new Point(100, 100),
            Size = new Size(120, 35),
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Lime,
            ForeColor = Color.Black,
            Font = new Font("Microsoft YaHei", 10, FontStyle.Bold)
        };

        nameDialog.Controls.Add(label);
        nameDialog.Controls.Add(textBox);
        nameDialog.Controls.Add(btnOk);
        nameDialog.AcceptButton = btnOk;

        if (nameDialog.ShowDialog() == DialogResult.OK)
        {
            string name = textBox.Text.Trim();
            if (string.IsNullOrEmpty(name)) name = $"Robot-{_robotIdCounter:D3}";
            SpawnRobot(name, -1, -1);
        }
    }

    public void SpawnRobot(string name, float startX, float startY)
    {
        int screenWidth = Screen.PrimaryScreen!.Bounds.Width;

        if (startX < 0)
        {
            startX = new Random().Next(screenWidth - 100);
            startY = -80;
        }

        Robot robot = new Robot(_robotIdCounter, name, startX, startY);
        robot.Size = DefaultRobotSize + new Random().Next(-10, 10);
        robot.SpeedMultiplier = _globalSpeed / 100f;

        _robots.Add(robot);
        _robotIdCounter++;

        robot.OnGrowthUpdated += (r) => PersistenceManager.SaveRobots(_robots);
        SkillManager.SaveRobotSkills(robot);

        PersistenceManager.SaveRobots(_robots);
        ShowNotification($"Robot '{name}' deployed!");
    }

    private void RestoreRobot(RobotData data)
    {
        int screenWidth = Screen.PrimaryScreen!.Bounds.Width;
        int screenHeight = Screen.PrimaryScreen!.Bounds.Height;
        float x = new Random().Next(screenWidth - 100);
        float y = new Random().Next(screenHeight - 100);

        Robot robot = new Robot(data.Id, data.Name, x, y);
        robot.Personality = data.Personality;
        robot.ConsciousnessLevel = data.ConsciousnessLevel;
        robot.Experience = data.Experience;
        robot.InternalGuidelines = data.InternalGuidelines;
        robot.Size = data.Size;
        robot.SpeedMultiplier = data.SpeedMultiplier;
        robot.PrimaryColor = Color.FromArgb(data.PrimaryColorR, data.PrimaryColorG, data.PrimaryColorB);
        
        // 优先加载专门的技能文件
        var savedSkills = SkillManager.LoadRobotSkills(data.Id, data.Name);
        if (savedSkills != null && savedSkills.Count > 0)
        {
            robot.Skills = savedSkills;
        }
        else if (data.Skills != null && data.Skills.Count > 0)
        {
            robot.Skills = data.Skills;
        }

        foreach (var insight in data.LearnedInsights) robot.LearnedInsights.Add(insight);

        robot.OnGrowthUpdated += (r) => PersistenceManager.SaveRobots(_robots);

        _robots.Add(robot);
    }

    public void ClearAllRobots()
    {
        _robots.Clear();
        PersistenceManager.SaveRobots(_robots);
    }

    public void RemoveRobot(Robot robot)
    {
        if (_robots.Contains(robot))
        {
            robot.CloseTerminal();
            _robots.Remove(robot);
            PersistenceManager.SaveRobots(_robots);
        }
    }

    public void ShowSettings()
    {
        if (_settingsForm == null || _settingsForm.IsDisposed)
        {
            _settingsForm = new SettingsForm();
            _settingsForm.RobotSize = DefaultRobotSize;
            _settingsForm.RobotName = DefaultRobotName;
            _settingsForm.RobotSpeed = _globalSpeed;
            _settingsForm.ShowNamingDialog = ShowNamingDialog;
            
            _settingsForm.FormClosed += (sender, args) =>
            {
                if (_settingsForm.DialogResult == DialogResult.OK)
                {
                    DefaultRobotSize = _settingsForm.RobotSize;
                    DefaultRobotName = _settingsForm.RobotName;
                    _globalSpeed = _settingsForm.RobotSpeed;
                    ShowNamingDialog = _settingsForm.ShowNamingDialog;
                    foreach (var r in _robots) r.SpeedMultiplier = _globalSpeed / 100f;
                }
                _settingsForm = null;
            };
            _settingsForm.Show();
        }
        else
        {
            _settingsForm.Activate();
        }
    }

    private void ShowControlPanel()
    {
        if (_controlPanel == null || _controlPanel.IsDisposed)
        {
            _controlPanel = new ControlPanelForm(this);
            _controlPanel.FormClosed += (s, e) => _controlPanel = null;
            _controlPanel.Show();
        }
        else
        {
            _controlPanel.Activate();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // 注销全局热键
        UnregisterHotKey(this.Handle, HOTKEY_ID_PAUSE);

        _moveTimer?.Stop();
        _moveTimer?.Dispose();

        _notifyIcon?.Dispose();
        base.OnFormClosing(e);
    }
}
