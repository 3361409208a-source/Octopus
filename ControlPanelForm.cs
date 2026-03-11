using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CockroachPet;

public class ControlPanelForm : Form
{
    private Form1 _mainForm;
    private ListView _robotListView;
    private System.Windows.Forms.Timer _updateTimer;

    public ControlPanelForm(Form1 mainForm)
    {
        _mainForm = mainForm;
        InitializeComponent();
        InitializeTimer();
    }

    private void InitializeComponent()
    {
        this.Text = "🤖 Robot Pet Control Panel";
        this.Size = new Size(700, 500);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.ForeColor = Color.White;
        this.MinimumSize = new Size(600, 400);

        // 标题
        var titleLabel = new Label
        {
            Text = "🤖 Pixel Robot Pet - Control Panel",
            Dock = DockStyle.Top,
            Height = 50,
            Font = new Font("Microsoft YaHei", 16, FontStyle.Bold),
            ForeColor = Color.Lime,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(40, 40, 40)
        };

        // 机器人列表
        _robotListView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.White,
            Font = new Font("Consolas", 10)
        };

        _robotListView.Columns.Add("ID", 50);
        _robotListView.Columns.Add("名称", 120);
        _robotListView.Columns.Add("状态", 80);
        _robotListView.Columns.Add("终端", 80);
        _robotListView.Columns.Add("可见", 60);
        _robotListView.Columns.Add("位置", 120);
        _robotListView.Columns.Add("速度", 80);
        _robotListView.Columns.Add("大小", 60);

        _robotListView.MouseDoubleClick += RobotListView_MouseDoubleClick;

        // 底部按钮面板
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.FromArgb(40, 40, 40),
            Padding = new Padding(10)
        };

        var btnSpawn = CreateButton("➕ 投放机器人", Color.Lime);
        btnSpawn.Click += (s, e) => _mainForm.SpawnRobotWithName();

        var btnQuickSpawn = CreateButton("⚡ 快速投放", Color.Cyan);
        btnQuickSpawn.Click += (s, e) =>
        {
            string[] names = { "Claude", "Alpha", "Beta", "Gamma", "Delta", "Neo" };
            _mainForm.SpawnRobot(names[new Random().Next(names.Length)], -1, -1);
        };

        var btnPauseAll = CreateButton("⏸ 全部暂停", Color.Yellow);
        btnPauseAll.Click += (s, e) =>
        {
            foreach (var r in _mainForm.GetRobots()) r.IsMoving = false;
        };

        var btnResumeAll = CreateButton("▶ 全部启动", Color.Lime);
        btnResumeAll.Click += (s, e) =>
        {
            foreach (var r in _mainForm.GetRobots()) r.IsMoving = true;
        };

        var btnClearAll = CreateButton("🗑️ 清除全部", Color.Red);
        btnClearAll.Click += (s, e) =>
        {
            if (MessageBox.Show("确定要清除所有机器人吗？", "确认", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _mainForm.ClearAllRobots();
                UpdateRobotList();
            }
        };

        var btnSettings = CreateButton("⚙️ 设置", Color.Orange);
        btnSettings.Click += (s, e) => _mainForm.ShowSettings();

        buttonPanel.Controls.Add(btnSpawn);
        buttonPanel.Controls.Add(btnQuickSpawn);
        buttonPanel.Controls.Add(btnPauseAll);
        buttonPanel.Controls.Add(btnResumeAll);
        buttonPanel.Controls.Add(btnClearAll);
        buttonPanel.Controls.Add(btnSettings);

        // 信息标签
        var infoLabel = new Label
        {
            Text = "💡 双击机器人打开/显示终端 | 右键查看更多操作",
            Dock = DockStyle.Bottom,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Gray,
            BackColor = Color.FromArgb(40, 40, 40)
        };

        this.Controls.Add(_robotListView);
        this.Controls.Add(buttonPanel);
        this.Controls.Add(infoLabel);
        this.Controls.Add(titleLabel);

        // 右键菜单
        var contextMenu = new ContextMenuStrip();
        contextMenu.Opening += (s, e) =>
        {
            contextMenu.Items.Clear();
            if (_robotListView.SelectedItems.Count > 0)
            {
                var robot = _robotListView.SelectedItems[0].Tag as Robot;
                if (robot != null)
                {
                    contextMenu.Items.Add("📺 打开终端", null, (s2, e2) => robot.OpenTerminal());
                    contextMenu.Items.Add("🗕 关闭终端", null, (s2, e2) => robot.CloseTerminal());
                    contextMenu.Items.Add(new ToolStripSeparator());
                    var status = robot.IsMoving ? "⏸ 暂停" : "▶ 启动";
                    contextMenu.Items.Add(status, null, (s2, e2) => robot.IsMoving = !robot.IsMoving);
                }
            }
        };
        _robotListView.ContextMenuStrip = contextMenu;
    }

    private Button CreateButton(string text, Color color)
    {
        return new Button
        {
            Text = text,
            Width = 100,
            Height = 35,
            FlatStyle = FlatStyle.Flat,
            BackColor = color,
            ForeColor = Color.Black,
            Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
            Margin = new Padding(5)
        };
    }

    private void InitializeTimer()
    {
        _updateTimer = new System.Windows.Forms.Timer();
        _updateTimer.Interval = 500;
        _updateTimer.Tick += (s, e) => UpdateRobotList();
        _updateTimer.Start();
    }

    private void UpdateRobotList()
    {
        _robotListView.Items.Clear();
        foreach (var robot in _mainForm.GetRobots())
        {
            var item = new ListViewItem(robot.Id.ToString());
            item.SubItems.Add(robot.Name);
            item.SubItems.Add(robot.IsMoving ? "▶ 移动中" : "⏸ 已暂停");
            
            // 终端状态（统一管理，不再单独显示）
            item.SubItems.Add("-");
            item.SubItems.Add("-");
            
            item.SubItems.Add($"({robot.X:F0}, {robot.Y:F0})");
            item.SubItems.Add($"{robot.SpeedMultiplier:F1}x");
            item.SubItems.Add($"{robot.Size}px");
            item.Tag = robot;
            _robotListView.Items.Add(item);
        }
    }

    private void RobotListView_MouseDoubleClick(object? sender, MouseEventArgs e)
    {
        if (_robotListView.SelectedItems.Count > 0)
        {
            var robot = _robotListView.SelectedItems[0].Tag as Robot;
            robot?.OpenTerminal();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
        base.OnFormClosing(e);
    }
}
