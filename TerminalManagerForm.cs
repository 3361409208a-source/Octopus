using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CockroachPet;

public class TerminalManagerForm : Form
{
    private TabControl _tabControl;
    private Dictionary<Robot, ChatTab> _terminals = new Dictionary<Robot, ChatTab>();
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
        this.Text = "💬 机器人聊天室";
        this.Size = new Size(500, 600);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(30, 30, 30);

        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei", 10)
        };

        this.Controls.Add(_tabControl);
        this.FormClosing += (s, e) => { e.Cancel = true; this.Hide(); };
    }

    public void OpenTerminal(Robot robot)
    {
        if (_terminals.ContainsKey(robot))
        {
            _tabControl.SelectedTab = _terminals[robot].TabPage;
        }
        else
        {
            var tab = new ChatTab(robot);
            _terminals[robot] = tab;
            _tabControl.TabPages.Add(tab.TabPage);
            _tabControl.SelectedTab = tab.TabPage;
        }
        this.Show();
        this.Activate();
    }

    public void CloseTerminal(Robot robot)
    {
        if (_terminals.ContainsKey(robot))
        {
            var tab = _terminals[robot];
            _tabControl.TabPages.Remove(tab.TabPage);
            _terminals.Remove(robot);
        }
    }
}

public class ChatTab
{
    private Robot _robot;
    private TabPage _tabPage;
    private FlowLayoutPanel _messagePanel;
    private TextBox _inputBox;

    public TabPage TabPage => _tabPage;

    public ChatTab(Robot robot)
    {
        _robot = robot;
        _tabPage = new TabPage { Text = $"  {robot.Name}  ", BackColor = Color.FromArgb(30, 30, 30) };
        
        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

        _messagePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(20, 20, 20),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(10)
        };
        _messagePanel.SizeChanged += (s, e) => {
            foreach (Control c in _messagePanel.Controls) c.Width = _messagePanel.ClientSize.Width - 25;
        };

        _inputBox = new TextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Microsoft YaHei", 11)
        };

        _inputBox.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendMessage();
                e.SuppressKeyPress = true;
            }
        };

        mainLayout.Controls.Add(_messagePanel, 0, 0);
        mainLayout.Controls.Add(_inputBox, 0, 1);
        _tabPage.Controls.Add(mainLayout);

        _robot.OnChatMessageReceived += HandleChatMessage;
        
        // 加载历史（历史消息目前不带思考过程，仅显示回复）
        foreach(var msg in _robot.ChatHistory)
        {
            HandleChatMessage(msg.role, msg.content, "");
        }
    }

    private void SendMessage()
    {
        string text = _inputBox.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        
        _inputBox.Clear();
        _robot.SendUserMessage(text).ConfigureAwait(false);
    }

    private void HandleChatMessage(string role, string content, string thought)
    {
        if (_messagePanel.InvokeRequired)
        {
            _messagePanel.Invoke(new Action(() => HandleChatMessage(role, content, thought)));
            return;
        }

        // 使用 TableLayoutPanel 代替 Panel，这是 WinForms 处理纵向堆叠最稳健的方式
        var msgContainer = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 0, // 动态增加
            Width = _messagePanel.ClientSize.Width - 30,
            AutoSize = true,
            BackColor = Color.FromArgb(35, 35, 35),
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 15)
        };
        msgContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        // 1. 名字头部
        var header = new Label
        {
            Text = role == "user" ? " 我:" : $" {_robot.Name}:",
            ForeColor = role == "user" ? Color.Cyan : Color.Gold,
            Font = new Font("Microsoft YaHei", 9, FontStyle.Bold),
            AutoSize = true,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 5)
        };
        msgContainer.Controls.Add(header);

        // 2. 思考过程 (如果有)
        if (!string.IsNullOrEmpty(thought))
        {
            var thoughtLabel = new Label
            {
                Text = thought,
                ForeColor = Color.DarkGray,
                BackColor = Color.FromArgb(25, 25, 25),
                Font = new Font("Consolas", 9),
                AutoSize = true,
                Dock = DockStyle.Top,
                Visible = false, // 默认折叠
                Padding = new Padding(8),
                Margin = new Padding(10, 5, 0, 5)
            };

            var toggleBtn = new Label
            {
                Text = " 💭 思考过程 (点击展开)",
                ForeColor = Color.Gray,
                Font = new Font("Microsoft YaHei", 8, FontStyle.Italic),
                Cursor = Cursors.Hand,
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 2, 0, 2)
            };

            toggleBtn.Click += (s, e) =>
            {
                thoughtLabel.Visible = !thoughtLabel.Visible;
                toggleBtn.Text = thoughtLabel.Visible ? " 💭 思考过程 (点击折叠)" : " 💭 思考过程 (点击展开)";
                // TableLayoutPanel 会因为 AutoSize 自动重绘
            };

            msgContainer.Controls.Add(toggleBtn);
            msgContainer.Controls.Add(thoughtLabel);
        }

        // 3. 正文内容
        var textBody = new Label
        {
            Text = content,
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei", 10),
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(5, 5, 0, 0)
        };
        msgContainer.Controls.Add(textBody);

        // 核心：强制让父容器下的所有 Label 在 TableLayoutPanel 里触发换行
        msgContainer.Paint += (s, e) => {
            if (header.Width != msgContainer.Width - 20) {
                header.MaximumSize = new Size(msgContainer.Width - 20, 0);
                textBody.MaximumSize = new Size(msgContainer.Width - 20, 0);
                // 思考内容也需要限制
                foreach (Control c in msgContainer.Controls) {
                    if (c is Label l && c != header && c != textBody)
                        l.MaximumSize = new Size(msgContainer.Width - 30, 0);
                }
            }
        };

        _messagePanel.Controls.Add(msgContainer);
        
        // 自动滚动
        _messagePanel.ScrollControlIntoView(msgContainer);
        _messagePanel.PerformLayout();
    }
}
