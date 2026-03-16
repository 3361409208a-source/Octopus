using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CockroachPet;

public class Robot
{
    // 基本信息
    public string Name { get; set; }
    public int Id { get; set; }

    // 位置
    public float X { get; set; }
    public float Y { get; set; }

    // 速度
    public float Dx { get; set; }
    public float Dy { get; set; }

    // 朝向
    public bool FacingRight { get; set; } = true;

    // 状态
    public bool IsMoving { get; set; } = true;
    public bool IsActive { get; set; } = true;

    // 动画
    public int AnimationFrame { get; set; } = 0;
    public int AnimationCounter { get; set; } = 0;

    // 大小
    public int Size { get; set; } = 64;

    // 速度倍率
    public float SpeedMultiplier { get; set; } = 1.0f;

    // 终端状态
    public string LastOutput { get; set; } = "";
    public string StatusMessage { get; set; } = "IDLE";
    public string AlertMessage { get; set; } = "";
    public bool IsWarning { get; set; } = false;
    public int WarningTimer { get; set; } = 0;
    public event Action<string>? OnTerminalOutput;
    
    // 特殊动画状态
    public string SpecialState { get; set; } = "NORMAL"; // NORMAL, HEART_EYES, SPINNING, BLUSH, SLEEPY
    public int SpecialStateTimer { get; set; } = 0;
    public float RotationAngle { get; set; } = 0;
    public int EmojiBubbleTimer { get; set; } = 0;
    public string CurrentEmoji { get; set; } = "";

    // 社交交互
    public string ChatText { get; set; } = "";
    private string _fullChatText = "";
    private int _streamCounter = 0;
    public int ChatTimer { get; set; } = 0;
    public int SocialCooldown { get; set; } = 0;
    public Robot? FollowingTarget { get; set; }
    public int FollowTimer { get; set; } = 0;

    // AI 独立体状态
    public int AiThoughtTimer { get; set; } = 1200; // 约 40s 一个想法
    private bool _isThinking = false;
    public bool IsThinking => _isThinking;
    public string LastAiThought { get; set; } = "";
    public string Personality { get; set; } = "Friendly"; // Curious, Grumpy, Energetic, Philosophical, etc.
    public List<(string role, string content)> ChatHistory { get; } = new List<(string, string)>();
    public event Action<string, string, string>? OnChatMessageReceived; // 角色, 内容, 思考过程

    // 随机行为
    public int PauseTimer { get; set; } = 0;
    public int ChangeDirectionTimer { get; set; } = 0;
    public Random Rand { get; set; } = new Random();

    // 颜色主题
    public Color PrimaryColor { get; set; }
    public Color SecondaryColor { get; set; }
    public Color EyeColor { get; set; }

    // 八爪鱼触手动画
    public float[] TentacleOffsets { get; set; } = new float[8];

    public Robot(int id, string name, float x, float y)
    {
        Id = id;
        Name = name;
        X = x;
        Y = y;

        // 随机颜色主题
        var colors = new (Color primary, Color secondary, Color eye)[]
        {
            (Color.FromArgb(255, 107, 107), Color.FromArgb(255, 77, 77), Color.FromArgb(255, 255, 255)), // 红色
            (Color.FromArgb(77, 171, 255), Color.FromArgb(51, 153, 255), Color.FromArgb(255, 255, 0)),   // 蓝色
            (Color.FromArgb(107, 255, 107), Color.FromArgb(77, 221, 77), Color.FromArgb(255, 100, 100)), // 绿色
            (Color.FromArgb(255, 200, 77), Color.FromArgb(255, 170, 51), Color.FromArgb(100, 50, 255)),  // 黄色
            (Color.FromArgb(200, 107, 255), Color.FromArgb(170, 77, 221), Color.FromArgb(0, 255, 255)),  // 紫色
            (Color.FromArgb(255, 150, 200), Color.FromArgb(255, 120, 170), Color.FromArgb(0, 0, 0)),     // 粉色
        };
        var theme = colors[Rand.Next(colors.Length)];
        PrimaryColor = theme.primary;
        SecondaryColor = theme.secondary;
        EyeColor = theme.eye;

        // 随机个性
        string[] personalities = { "好奇", "傲娇", "热血", "深沉", "社牛", "摆烂", "热心肠", "赛博朋克" };
        Personality = personalities[Rand.Next(personalities.Length)];

        // 随机初始方向
        double angle = Rand.NextDouble() * Math.PI * 2;
        float speed = 1.5f + Rand.NextFloat() * 1.5f;
        Dx = (float)Math.Cos(angle) * speed;
        Dy = (float)Math.Sin(angle) * speed;
    }

    public void Update(int screenWidth, int screenHeight)
    {
        if (WarningTimer > 0)
        {
            WarningTimer--;
            if (WarningTimer == 0) IsWarning = false;
        }

        if (!IsActive || !IsMoving) return;

        // 停顿逻辑
        if (PauseTimer > 0)
        {
            PauseTimer--;
            UpdateTentacles(true); // 停顿时的触手动画
            return;
        }

        // 随机停顿
        if (Rand.Next(1000) < 3)
        {
            PauseTimer = Rand.Next(30, 90);
            return;
        }

        // 随机改变方向
        ChangeDirectionTimer++;
        if (ChangeDirectionTimer > 120 && Rand.Next(100) < 5)
        {
            ChangeDirectionTimer = 0;
            double angle = Rand.NextDouble() * Math.PI * 2;
            float speed = (float)Math.Sqrt(Dx * Dx + Dy * Dy);
            Dx = (float)Math.Cos(angle) * speed;
            Dy = (float)Math.Sin(angle) * speed;
        }

        // 移动
        X += Dx * SpeedMultiplier;
        Y += Dy * SpeedMultiplier;

        // 边界检测
        if (X <= 0 || X >= screenWidth - Size)
        {
            Dx = -Dx;
            FacingRight = Dx > 0;
            X = Math.Max(0, Math.Min(X, screenWidth - Size));
        }

        if (Y <= 0 || Y >= screenHeight - Size)
        {
            Dy = -Dy;
            Y = Math.Max(0, Math.Min(Y, screenHeight - Size));
        }

        // 社交与跟随逻辑
        if (FollowTimer > 0 && FollowingTarget != null && FollowingTarget.IsActive)
        {
            FollowTimer--;
            // 缓慢靠近目标
            float targetDx = FollowingTarget.X - X;
            float targetDy = FollowingTarget.Y - Y;
            float dist = (float)Math.Sqrt(targetDx * targetDx + targetDy * targetDy);
            if (dist > 50)
            {
                Dx = (Dx * 0.95f) + (targetDx / dist * 0.1f * SpeedMultiplier);
                Dy = (Dy * 0.95f) + (targetDy / dist * 0.1f * SpeedMultiplier);
            }
            if (FollowTimer == 0) FollowingTarget = null;
        }

        if (SocialCooldown > 0) SocialCooldown--;
        if (ChatTimer > 0) ChatTimer--;
        
        // 流式文字逻辑
        UpdateStreamingChat();

        // AI 思考逻辑
        UpdateAiThinking();

        // 更新朝向
        if (Dx > 0.1f) FacingRight = true;
        else if (Dx < -0.1f) FacingRight = false;

        // 更新动画
        AnimationCounter++;
        if (AnimationCounter >= 8)
        {
            AnimationCounter = 0;
            AnimationFrame = (AnimationFrame + 1) % 4;
        }

        UpdateSpecialAnimations();
        UpdateTentacles(false);
    }

    private void UpdateSpecialAnimations()
    {
        // 特殊状态更新
        if (SpecialStateTimer > 0)
        {
            SpecialStateTimer--;
            if (SpecialState == "SPINNING")
            {
                RotationAngle += 15f;
            }
            if (SpecialStateTimer == 0)
            {
                SpecialState = "NORMAL";
                RotationAngle = 0;
            }
        }
        else if (Rand.Next(1000) < 5) // 0.5% 概率触发随机动画
        {
            string[] states = { "HEART_EYES", "SPINNING", "BLUSH", "SLEEPY" };
            SpecialState = states[Rand.Next(states.Length)];
            SpecialStateTimer = Rand.Next(60, 180);
        }

        // 表情气泡逻辑
        if (EmojiBubbleTimer > 0)
        {
            EmojiBubbleTimer--;
        }
        else if (Rand.Next(2000) < 3)
        {
            string[] emojis = { "☕", "💡", "🎮", "🎵", "🍕", "⭐", "🔥", "💨" };
            CurrentEmoji = emojis[Rand.Next(emojis.Length)];
            EmojiBubbleTimer = Rand.Next(60, 120);
        }
    }

    private void UpdateAiThinking()
    {
        if (_isThinking || !IsActive) return;

        if (AiThoughtTimer > 0)
        {
            AiThoughtTimer--;
        }
        else
        {
            AiThoughtTimer = Rand.Next(1500, 3000); // 下一次启动时间随机
            TriggerAiThought();
        }
    }

    private async void TriggerAiThought()
    {
        _isThinking = true;
        
        string currentActivity = IsMoving ? (FollowingTarget != null ? "Following friend" : "Exploring") : "Resting";
        if (IsWarning) currentActivity = "Alerted: " + AlertMessage;
        
        var thought = await AiService.GetThoughtAsync(Name, StatusMessage, currentActivity, Personality);
        
        _isThinking = false;

        if (!string.IsNullOrEmpty(thought) && IsActive)
        {
            LastAiThought = thought;
            _fullChatText = thought;
            ChatText = "";
            _streamCounter = 0;
            ChatTimer = 180 + thought.Length * 5; // 根据长度增加显示时间
            System.Diagnostics.Debug.WriteLine($"[Robot {Name}] AI Thought: {thought}");
        }
    }

    private void UpdateStreamingChat()
    {
        if (ChatText.Length < _fullChatText.Length)
        {
            _streamCounter++;
            if (_streamCounter >= 2) // 每 2 帧出一个字
            {
                ChatText = _fullChatText.Substring(0, ChatText.Length + 1);
                _streamCounter = 0;
            }
        }
    }

    public async Task SendUserMessage(string message)
    {
        if (_isThinking) return;
        
        OnChatMessageReceived?.Invoke("user", message, "");
        ChatHistory.Add(("user", message));
        if (ChatHistory.Count > 10) ChatHistory.RemoveAt(0);

        _isThinking = true;
        // 视觉提示
        _fullChatText = "想着呢...";
        ChatText = "";
        _streamCounter = 0;
        ChatTimer = 60;

        AiService.ChatResponse result = await AiService.GetChatResponseAsync(Name, Personality, message, ChatHistory);
        string thought = result.Thought;
        string response = result.Answer;
        
        _isThinking = false;
        ChatHistory.Add(("assistant", response));
        if (ChatHistory.Count > 10) ChatHistory.RemoveAt(0);

        _fullChatText = response;
        ChatText = "";
        _streamCounter = 0;
        ChatTimer = 180 + response.Length * 5;
        OnChatMessageReceived?.Invoke("assistant", response, thought);
    }

    public void InteractWith(Robot other)
    {
        if (SocialCooldown > 0 || other.SocialCooldown > 0) return;

        float dx = other.X - X;
        float dy = other.Y - Y;
        float dist = (float)Math.Sqrt(dx * dx + dy * dy);

        if (dist < 40) // 非常近：碰撞反弹或打招呼
        {
            if (Rand.Next(10) < 3) // 30% 概率打招呼
            {
                SayHi(other);
            }
            else // 物理碰撞反弹
            {
                float tempDx = Dx;
                float tempDy = Dy;
                Dx = other.Dx;
                Dy = other.Dy;
                other.Dx = tempDx;
                other.Dy = tempDy;
                
                // 碰撞动画
                SpecialState = "SPINNING";
                SpecialStateTimer = 30;
                other.SpecialState = "SPINNING";
                other.SpecialStateTimer = 30;
            }
            SocialCooldown = 60;
            other.SocialCooldown = 60;
        }
        else if (dist < 150 && FollowingTarget == null && Rand.Next(500) < 2) // 较近：触发跟随
        {
            FollowingTarget = other;
            FollowTimer = Rand.Next(100, 300);
            ChatText = $"Wait for me, {other.Name}!";
            ChatTimer = 90;
        }
    }

    private void SayHi(Robot other)
    {
        string[] greetings = { "Hi!", "Hello~", "Yo!", "Nice to meet ya", "hey!", "o/" };
        ChatText = greetings[Rand.Next(greetings.Length)] + " " + other.Name;
        ChatTimer = 120;
        PauseTimer = 60;
        
        // 对方也可能回应
        if (Rand.Next(2) == 0)
        {
            other.ChatText = greetings[Rand.Next(greetings.Length)] + " " + Name;
            other.ChatTimer = 120;
            other.PauseTimer = 60;
        }
        
        SpecialState = "BLUSH";
        SpecialStateTimer = 60;
    }

    private void UpdateTentacles(bool idle)
    {
        float speed = idle ? 0.1f : 0.3f;
        for (int i = 0; i < 8; i++)
        {
            TentacleOffsets[i] += speed + Rand.NextFloat() * 0.1f;
        }
    }

    public bool HitTest(int mx, int my)
    {
        return mx >= X && mx <= X + Size &&
               my >= Y && my <= Y + Size;
    }

    public void OpenTerminal()
    {
        // 使用统一的终端管理器
        TerminalManagerForm.Instance.OpenTerminal(this);
    }
    
    public void CloseTerminal()
    {
        // 关闭该机器人的终端标签页
        TerminalManagerForm.Instance.CloseTerminal(this);
    }

    public void NotifyOutput(string text, bool isError = false)
    {
        LastOutput = text;
        OnTerminalOutput?.Invoke(text);
        
        // 1. 如果是明显的错误流信息 (StandardError)
        if (isError)
        {
            StatusMessage = "ERROR";
            AlertMessage = "SOMETHING BROKE!";
            IsWarning = true;
            WarningTimer = 180;
            return;
        }

        // 2. 检测 Claude 或终端常见的确认/输入请求
        if (text.Contains("(y/n)", StringComparison.OrdinalIgnoreCase) || 
            text.Contains("[y/n]", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Confirm?", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Proceed?", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Continue?", StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "WAITING";
            AlertMessage = "CLAUDE NEEDS YOU!";
            IsWarning = true;
            WarningTimer = 300; 
        }
        // 3. 增强的错误关键词检测（包括您截图中的情况）
        else if (text.Contains("Error", StringComparison.OrdinalIgnoreCase) || 
                 text.Contains("not recognized", StringComparison.OrdinalIgnoreCase) ||
                 text.Contains("failed", StringComparison.OrdinalIgnoreCase)) 
        {
            StatusMessage = "ERROR";
            AlertMessage = "SOMETHING BROKE!";
            IsWarning = true;
            WarningTimer = 180;
        }
        else if (text.Contains("Finished", StringComparison.OrdinalIgnoreCase)) StatusMessage = "COMPLETED";
        else if (text.Contains("Running", StringComparison.OrdinalIgnoreCase)) StatusMessage = "BUSY";
    }

    public void NotifyAiToolStarted(string toolName, int processId)
    {
        StatusMessage = $"RUNNING {toolName.ToUpper()}";
        AlertMessage = $"USING {toolName.ToUpper()}!";
        IsWarning = false;
        WarningTimer = 60;
        System.Diagnostics.Debug.WriteLine($"[Robot {Name}] AI tool started: {toolName} (PID: {processId})");
    }
}

public static class RandomExtensions
{
    public static float NextFloat(this Random rand)
    {
        return (float)rand.NextDouble();
    }
}
