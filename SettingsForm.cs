using System;
using System.Drawing;
using System.Windows.Forms;

namespace CockroachPet;

public class SettingsForm : Form
{
    // 设置值
    public int RobotCount { get; set; } = 1;
    public bool ShowNamingDialog { get; set; } = true;
    public int RobotSize { get; set; } = 64;
    public int RobotSpeed { get; set; } = 100;
    public string RobotName { get; set; } = "Claude";
    public bool AutoStart { get; set; } = false;

    private NumericUpDown _countInput;
    private NumericUpDown _sizeInput;
    private NumericUpDown _speedInput;
    private TextBox _nameInput;
    private CheckBox _namingCheck;
    private CheckBox _autoStartCheck;

    public SettingsForm()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        this.Text = "Robot Pet Settings";
        this.Size = new Size(450, 400);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(40, 40, 40);
        this.ForeColor = Color.White;
        this.Font = new Font("Microsoft YaHei", 10);

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            RowCount = 8,
            ColumnCount = 2,
            BackColor = Color.FromArgb(40, 40, 40)
        };

        // 标题
        var titleLabel = new Label
        {
            Text = "⚙️ Robot Pet Settings",
            Font = new Font("Microsoft YaHei", 16, FontStyle.Bold),
            ForeColor = Color.Lime,
            Dock = DockStyle.Top,
            Height = 40,
            TextAlign = ContentAlignment.MiddleCenter
        };

        // 机器人数量
        panel.Controls.Add(CreateLabel("机器人数量:"), 0, 0);
        _countInput = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 10,
            Value = 1,
            Width = 100,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        panel.Controls.Add(_countInput, 1, 0);

        // 显示命名对话框
        panel.Controls.Add(CreateLabel("命名对话框:"), 0, 1);
        _namingCheck = new CheckBox
        {
            Text = "启动时询问命名",
            Checked = false, // 默认不询问
            ForeColor = Color.White,
            AutoSize = true
        };
        panel.Controls.Add(_namingCheck, 1, 1);

        // 默认名字
        panel.Controls.Add(CreateLabel("默认名字:"), 0, 2);
        _nameInput = new TextBox
        {
            Text = "Claude",
            Width = 150,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        panel.Controls.Add(_nameInput, 1, 2);

        // 默认大小
        panel.Controls.Add(CreateLabel("默认大小 (px):"), 0, 3);
        _sizeInput = new NumericUpDown
        {
            Minimum = 32,
            Maximum = 128,
            Value = 64,
            Width = 100,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };
        panel.Controls.Add(_sizeInput, 1, 3);

        // 默认速度
        panel.Controls.Add(CreateLabel("默认速度 (%):"), 0, 4);
        _speedInput = new NumericUpDown
        {
            Minimum = 50,
            Maximum = 300,
            Value = 100,
            Width = 100,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };
        panel.Controls.Add(_speedInput, 1, 4);

        // 自动启动
        panel.Controls.Add(CreateLabel("自动启动:"), 0, 5);
        _autoStartCheck = new CheckBox
        {
            Text = "设置后直接启动",
            Checked = false,
            ForeColor = Color.White,
            AutoSize = true
        };
        panel.Controls.Add(_autoStartCheck, 1, 5);

        // 说明
        var infoLabel = new Label
        {
            Text = "💡 提示: 左键点击机器人打开CMD终端\n    Ctrl+Shift+M 打开菜单 | Ctrl+Shift+P 暂停/继续",
            ForeColor = Color.Gray,
            Font = new Font("Microsoft YaHei", 9),
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 0)
        };
        panel.Controls.Add(infoLabel, 0, 6);
        panel.SetColumnSpan(infoLabel, 2);

        // 按钮
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.FromArgb(40, 40, 40)
        };

        var btnCancel = new Button
        {
            Text = "取消",
            Width = 80,
            Height = 32,
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(80, 80, 80),
            ForeColor = Color.White
        };

        var btnSave = new Button
        {
            Text = "保存",
            Width = 100,
            Height = 32,
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Lime,
            ForeColor = Color.Black,
            Font = new Font("Microsoft YaHei", 10, FontStyle.Bold)
        };

        buttonPanel.Controls.Add(btnCancel);
        buttonPanel.Controls.Add(btnSave);

        this.Controls.Add(panel);
        this.Controls.Add(buttonPanel);
        this.Controls.Add(titleLabel);

        this.AcceptButton = btnSave;
        this.CancelButton = btnCancel;
    }

    private Label CreateLabel(string text)
    {
        return new Label
        {
            Text = text,
            ForeColor = Color.White,
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            TextAlign = ContentAlignment.MiddleRight
        };
    }

    private void LoadSettings()
    {
        // 从文件加载设置（如果有）
        try
        {
            string settingsPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "RobotPetSettings.txt");
            if (System.IO.File.Exists(settingsPath))
            {
                var lines = System.IO.File.ReadAllLines(settingsPath);
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        switch (parts[0])
                        {
                            case "Count": _countInput.Value = int.Parse(parts[1]); break;
                            case "ShowNaming": _namingCheck.Checked = bool.Parse(parts[1]); break;
                            case "DefaultName": _nameInput.Text = parts[1]; break;
                            case "DefaultSize": _sizeInput.Value = int.Parse(parts[1]); break;
                            case "DefaultSpeed": _speedInput.Value = int.Parse(parts[1]); break;
                            case "AutoStart": _autoStartCheck.Checked = bool.Parse(parts[1]); break;
                        }
                    }
                }
            }
        }
        catch { }
    }

    public void SaveSettings()
    {
        RobotCount = (int)_countInput.Value;
        ShowNamingDialog = _namingCheck.Checked;
        RobotName = _nameInput.Text.Trim();
        RobotSize = (int)_sizeInput.Value;
        RobotSpeed = (int)_speedInput.Value;
        AutoStart = _autoStartCheck.Checked;

        // 保存到文件
        try
        {
            string settingsPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "RobotPetSettings.txt");
            var lines = new[]
            {
                $"Count={RobotCount}",
                $"ShowNaming={ShowNamingDialog}",
                $"DefaultName={RobotName}",
                $"DefaultSize={RobotSize}",
                $"DefaultSpeed={RobotSpeed}",
                $"AutoStart={AutoStart}"
            };
            System.IO.File.WriteAllLines(settingsPath, lines);
        }
        catch { }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (this.DialogResult == DialogResult.OK)
        {
            SaveSettings();
        }
        base.OnFormClosing(e);
    }
}
