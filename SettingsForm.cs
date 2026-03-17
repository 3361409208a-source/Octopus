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


    public SettingsForm()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        this.Text = "Robot Pet Settings";
        this.Size = new Size(500, 500);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(40, 40, 40);
        this.ForeColor = Color.White;
        this.Font = new Font("Microsoft YaHei", 10);

        // 创建主容器
        var mainContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(0),
            BackColor = Color.FromArgb(40, 40, 40)
        };
        mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 标题
        mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // 内容区域
        mainContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 按钮区域

        // 标题
        var titleLabel = new Label
        {
            Text = "⚙️ Robot Pet Settings",
            Font = new Font("Microsoft YaHei", 16, FontStyle.Bold),
            ForeColor = Color.Lime,
            Dock = DockStyle.Top,
            Height = 50,
            TextAlign = ContentAlignment.MiddleCenter
        };

        // 创建内容面板并设置滚动
        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(20),
            BackColor = Color.FromArgb(40, 40, 40)
        };

        var tableLayoutPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Padding = new Padding(0),
            RowCount = 7,
            ColumnCount = 2,
            BackColor = Color.FromArgb(40, 40, 40),
            AutoSize = true,
            MaximumSize = new Size(contentPanel.Width - 40, 0) // 减去滚动条宽度
        };
        tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        // 机器人数量
        tableLayoutPanel.Controls.Add(CreateLabel("机器人数量:"), 0, 0);
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
        tableLayoutPanel.Controls.Add(_countInput, 1, 0);

        // 显示命名对话框
        tableLayoutPanel.Controls.Add(CreateLabel("命名对话框:"), 0, 1);
        _namingCheck = new CheckBox
        {
            Text = "启动时询问命名",
            Checked = false, // 默认不询问
            ForeColor = Color.White,
            AutoSize = true
        };
        tableLayoutPanel.Controls.Add(_namingCheck, 1, 1);

        // 默认名字
        tableLayoutPanel.Controls.Add(CreateLabel("默认名字:"), 0, 2);
        _nameInput = new TextBox
        {
            Text = "Claude",
            Width = 150,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        tableLayoutPanel.Controls.Add(_nameInput, 1, 2);

        // 默认大小
        tableLayoutPanel.Controls.Add(CreateLabel("默认大小 (px):"), 0, 3);
        _sizeInput = new NumericUpDown
        {
            Minimum = 32,
            Maximum = 128,
            Value = 64,
            Width = 100,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };
        tableLayoutPanel.Controls.Add(_sizeInput, 1, 3);

        // 默认速度
        tableLayoutPanel.Controls.Add(CreateLabel("默认速度 (%):"), 0, 4);
        _speedInput = new NumericUpDown
        {
            Minimum = 50,
            Maximum = 300,
            Value = 100,
            Width = 100,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White
        };
        tableLayoutPanel.Controls.Add(_speedInput, 1, 4);

        // 自动启动
        tableLayoutPanel.Controls.Add(CreateLabel("自动启动:"), 0, 5);
        _autoStartCheck = new CheckBox
        {
            Text = "设置后直接启动",
            Checked = false,
            ForeColor = Color.White,
            AutoSize = true
        };
        tableLayoutPanel.Controls.Add(_autoStartCheck, 1, 5);





        // 说明标签
        var infoLabel = new Label
        {
            Text = "💡 提示: 左键点击机器人打开CMD终端\n    Ctrl+Shift+M 打开菜单 | Ctrl+Shift+P 暂停/继续",
            ForeColor = Color.Gray,
            Font = new Font("Microsoft YaHei", 9),
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 0)
        };
        tableLayoutPanel.Controls.Add(infoLabel, 0, 6);
        tableLayoutPanel.SetColumnSpan(infoLabel, 2);

        // 调整表格布局高度
        for (int i = 0; i < 7; i++)
        {
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        contentPanel.Controls.Add(tableLayoutPanel);

        // 按钮面板
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(20, 10, 20, 10),
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
        btnSave.Click += (s, e) => Console.WriteLine("[Settings] Save button clicked.");

        buttonPanel.Controls.Add(btnCancel);
        buttonPanel.Controls.Add(btnSave);

        // 添加控件到主容器
        mainContainer.Controls.Add(titleLabel, 0, 0);
        mainContainer.Controls.Add(contentPanel, 0, 1);
        mainContainer.Controls.Add(buttonPanel, 0, 2);

        this.Controls.Add(mainContainer);

        this.AcceptButton = btnSave;
        this.CancelButton = btnCancel;

        // 重新调整TableLayoutPanel的大小以适应内容
        tableLayoutPanel.ResumeLayout(false);
        tableLayoutPanel.PerformLayout();
        contentPanel.ResumeLayout(false);
        contentPanel.PerformLayout();
        mainContainer.ResumeLayout(false);
        mainContainer.PerformLayout();
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
                $"AutoStart={AutoStart}",

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
