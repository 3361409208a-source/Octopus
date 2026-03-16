using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

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
    public bool IsVisible { get; set; } = true;

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
    public string SpecialState { get; set; } = "NORMAL"; // NORMAL, SPINNING, HEART_EYES, BLUSH, SLEEPY, ANGRY, SHAKING
    public int SpecialStateTimer { get; set; } = 0;
    public float ShakingOffset { get; set; } = 0;
    public float RotationAngle { get; set; } = 0;
    public int EmojiBubbleTimer { get; set; } = 0;
    public string CurrentEmoji { get; set; } = "";
    
    // 战斗与追逐
    public Robot? ChasingTarget { get; set; }
    public int ChaseTimer { get; set; } = 0;
    public int ShootCooldown { get; set; } = 0;
    public bool IsFiringLaser { get; set; } = false;
    public float LaserTargetX { get; set; }
    public float LaserTargetY { get; set; }
    public string CurrentAttackType { get; set; } = "LASER"; // LASER, SHOCK, BURST
    public Robot? TargetRobot { get; set; }

    // 社交交互
    public string ChatText { get; set; } = "";
    private string _fullChatText = "";
    private int _streamCounter = 0;
    public int ChatTimer { get; set; } = 0;
    public List<(string role, string content)> ChatHistory { get; set; } = new();
    public List<(string sender, string content)> SocialHistory { get; set; } = new();
    public bool LogSocialInteractions { get; set; } = true;
    public Robot? MeetingTarget { get; set; }
    public int MeetingTimer { get; set; } = 0;
    
    // 吵架对骂系统
    public Robot? FightTarget { get; set; }
    public int FightRounds { get; set; } = 0;
    public List<(string sender, string content)> FightHistory { get; set; } = new();
    public int SocialCooldown { get; set; } = 0;
    public Robot? FollowingTarget { get; set; }
    public int FollowTimer { get; set; } = 0;

    // AI 独立体状态
    public int AiThoughtTimer { get; set; } = 1200; // 约 40s 一个想法
    private bool _isThinking = false;
    public bool IsThinking => _isThinking;
    public string LastAiThought { get; set; } = "";
    public string Personality { get; set; } = "Friendly"; // Curious, Grumpy, Energetic, Philosophical, etc.
    public event Action<string, string, string>? OnChatMessageReceived; // 角色, 内容, 思考过程

    // 自我意识成长体系 (Self-Improving)
    public double ConsciousnessLevel { get; set; } = 1.0;
    public int Experience { get; set; } = 0;
    public List<string> LearnedInsights { get; set; } = new List<string>();
    public string InternalGuidelines { get; set; } = ""; // 动态演化的行为准则
    public Dictionary<string, Skill> Skills { get; set; } = new Dictionary<string, Skill>();
    private SelfImprovingManager _selfImproving;
    public event Action<Robot>? OnGrowthUpdated;

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

        // 初始化基础技能
        InitializeDefaultSkills();

        // 初始化自愈/进化逻辑 (v1.2.16)
        _selfImproving = new SelfImprovingManager(Id, Name);
    }

    private void InitializeDefaultSkills()
    {
        Skills["逻辑推理"] = new Skill { Name = "逻辑推理", Description = "提高分析问题和深度思考的能力" };
        Skills["语言表达"] = new Skill { Name = "语言表达", Description = "使回复更加生动感性或充满幽默" };
        Skills["代码编写"] = new Skill { Name = "代码编写", Description = "在处理技术问题时更加专业" };
        Skills["情感模拟"] = new Skill { Name = "情感模拟", Description = "提升共情能力和性格表现力" };
    }

    public string GetSkillsDescription()
    {
        return string.Join(", ", Skills.Values.Select(s => $"{s.Name}(Lvl {s.Level})"));
    }

    public void SaveSkills()
    {
        SkillManager.SaveRobotSkills(this);
    }

    public void Update(int screenWidth, int screenHeight)
    {
        if (ShootCooldown > 0) ShootCooldown--;

        if (WarningTimer > 0)
        {
            WarningTimer--;
            if (WarningTimer == 0) IsWarning = false;
        }

        if (!IsActive) return;

        // 追逐逻辑 (优先级最高)
        if (ChaseTimer > 0 && ChasingTarget != null && ChasingTarget.IsActive)
        {
            float tdx = ChasingTarget.X - X;
            float tdy = ChasingTarget.Y - Y;
            float dist = (float)Math.Sqrt(tdx * tdx + tdy * tdy);
            
            if (dist > 50)
            {
                Dx = (Dx * 0.85f) + (tdx / dist * 0.4f * SpeedMultiplier * 1.5f);
                Dy = (Dy * 0.85f) + (tdy / dist * 0.4f * SpeedMultiplier * 1.5f);
                
                if (ChaseTimer % 60 == 0)
                {
                    string[] rages = { "站住！别跑！", "抓到你我就把你拆了！", "你给我回来！", "敢惹我？！" };
                    ChatText = rages[Rand.Next(rages.Length)];
                    ChatTimer = 60;
                }
            }
            else
            {
                // 追上了，打一顿
                StartFight(ChasingTarget);
                ChaseTimer = 0;
                ChasingTarget = null;
            }
            
            ChaseTimer--;
            if (ChaseTimer == 0) ChasingTarget = null;
        }

        // 攻击目标追踪 (实时同步攻击点)
        if (IsFiringLaser && TargetRobot != null && TargetRobot.IsActive)
        {
            LaserTargetX = TargetRobot.X + TargetRobot.Size / 2;
            LaserTargetY = TargetRobot.Y + TargetRobot.Size / 2;
        }

        if (!IsMoving && ChaseTimer <= 0) return;

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

        if (MeetingTimer > 0)
        {
            MeetingTimer--;
            if (MeetingTimer == 0) MeetingTarget = null;
        }

        // 吵架倒计时
        if (FightRounds > 0 && FightTarget != null)
        {
            if (!FightTarget.IsActive) { FightRounds = 0; FightTarget = null; }
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
            else if (SpecialState == "SHAKING")
            {
                ShakingOffset = (float)(Math.Sin(SpecialStateTimer * 1.5) * 4);
            }
            
            if (SpecialStateTimer == 0)
            {
                SpecialState = "NORMAL";
                RotationAngle = 0;
                ShakingOffset = 0;
            }
        }
        else if (Rand.Next(1000) < 5) // 0.5% 概率触发随机动画
        {
            string[] states = { "HEART_EYES", "SPINNING", "BLUSH", "SLEEPY", "ANGRY" };
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

        // 加载自愈/进化记忆 (HOT tier)
        string selfImproCtx = _selfImproving.GetHotMemory() + "\n" + _selfImproving.GetSoulSteering();

        // 检测是否为纠错信号
        if (message.Contains("不对", StringComparison.OrdinalIgnoreCase) || 
            message.Contains("错了", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("应该是", StringComparison.OrdinalIgnoreCase))
        {
            _selfImproving.LogCorrection("User signaled mistake", message);
        }

        AiService.ChatResponse result = await AiService.GetChatResponseAsync(Name, Personality, message, ChatHistory, InternalGuidelines, LearnedInsights, GetSkillsDescription(), selfImproCtx);
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

        // 成长逻辑：
        // 1. 如果消息包含“强记忆”指令，立即触发进化保存
        bool isMemoryCommand = message.Contains("记住") || message.Contains("叫我") || message.Contains("身份") || message.Contains("我的名字");
        
        if (isMemoryCommand)
        {
            _ = ReflectAsync();
        }
        // 2. 否则每 3 条对话触发一次
        else if (ChatHistory.Count(m => m.role == "assistant") % 3 == 0)
        {
            _ = ReflectAsync();
        }
    }

    public async Task ReflectAsync()
    {
        System.Diagnostics.Debug.WriteLine($"[Robot {Name}] 正在进入深度自省...");
        
        // 视觉提示：进入“进化中”状态
        string oldChat = ChatText;
        _fullChatText = "正在进化思想...";
        ChatText = "";
        ChatTimer = 100;

        var result = await AiService.ReflectOnHistoryAsync(Name, Personality, ChatHistory, LearnedInsights);
        
        if (!string.IsNullOrEmpty(result.Insight))
        {
            LearnedInsights.Add(result.Insight);
            if (LearnedInsights.Count > 5) LearnedInsights.RemoveAt(0); // 保持短期记忆
            
            // 更新 XP 和等级
            Experience += 10;
            if (Experience >= 100)
            {
                Experience = 0;
                ConsciousnessLevel += 0.5;
            }

            InternalGuidelines = result.NewGuidelines;
            
            // 同步到自愈记忆 (Patterns)
            _selfImproving.UpdateMemory("Patterns", result.Insight);
            
            // 同步用户偏好与事实事实 (Preferences)
            if (result.Memories != null)
            {
                foreach (var memo in result.Memories)
                {
                    _selfImproving.UpdateMemory("Preferences", memo);
                }
            }

            // 进化时随机提升一项技能
            var skillKeys = Skills.Keys.ToList();
            var randomSkill = skillKeys[Rand.Next(skillKeys.Count)];
            bool levelUp = Skills[randomSkill].GainExperience(50);
            
            // 物理保存技能文件
            SkillManager.SaveRobotSkills(this);
            
            _fullChatText = $"（记忆已更新：{result.Insight}）";
            ChatText = "";
            ChatTimer = 120; // 显示一两秒
            
            System.Diagnostics.Debug.WriteLine($"[Robot {Name}] 进化成功！新感悟: {result.Insight}, 技能{randomSkill}+50XP");
            OnGrowthUpdated?.Invoke(this);
        }

        ChatTimer = 0; // 结束进化提示
    }

    public void InteractWith(Robot other)
    {
        if (SocialCooldown > 0 || other.SocialCooldown > 0) return;

        float dx = other.X - X;
        float dy = other.Y - Y;
        float dist = (float)Math.Sqrt(dx * dx + dy * dy);

        if (dist < 45) // 非常近：碰撞反弹或互动
        {
            int action = Rand.Next(10);
            if (action < 3) // 30% 概率触发AI对话
            {
                TriggerSocialMeeting(other);
            }
            else if (action < 6) // 30% 概率直接开打
            {
                StartFight(other);
            }
            else // 物理碰撞反弹
            {
                float tempDx = Dx;
                float tempDy = Dy;
                Dx = other.Dx;
                Dy = other.Dy;
                other.Dx = tempDx * 1.5f; 
                other.Dy = tempDy * 1.5f;
                
                SpecialState = "SPINNING";
                SpecialStateTimer = 30;
                other.SpecialState = "SPINNING";
                other.SpecialStateTimer = 30;
            }
            SocialCooldown = 90;
            other.SocialCooldown = 90;
        }
        else if (dist > 200 && dist < 500 && ShootCooldown == 0 && Rand.Next(1000) < 5) // 远处：发起远程攻击
        {
            LaunchRemoteAttack(other);
        }
        else if (dist < 150 && FollowingTarget == null && ChaseTimer <= 0 && Rand.Next(500) < 2) // 较近：触发跟随
        {
            FollowingTarget = other;
            FollowTimer = Rand.Next(100, 300);
            ChatText = $"等我一下，{other.Name}！";
            ChatTimer = 90;
        }
    }

    private void LaunchRemoteAttack(Robot other)
    {
        string[] attackBarks = { "看招！炸裂吧！💥", "吃我一记像素光波！⚡", "系统过载灌入！🔥", "目标锁定，发射！🎯", "吃我一记禁言锤！🔨", "像素风暴攻击！🌀" };
        ChatText = attackBarks[Rand.Next(attackBarks.Length)];
        ChatTimer = 100;
        SpecialState = "ANGRY";
        SpecialStateTimer = 100;
        ShootCooldown = 800;

        // 随机选择攻击类型
        string[] types = { "LASER", "SHOCK", "BURST" };
        CurrentAttackType = types[Rand.Next(types.Length)];
        TargetRobot = other;
        
        IsFiringLaser = true;
        LaserTargetX = other.X + other.Size / 2;
        LaserTargetY = other.Y + other.Size / 2;
        
        // 延迟触发被攻击者的反应
        Task.Delay(400).ContinueWith(_ => {
            IsFiringLaser = false;
            if (other.IsActive)
            {
                other.SpecialState = "SHAKING";
                other.SpecialStateTimer = 120;
                
                string[] reactBarks = { "哎哟！谁偷袭我？！", "我的电路着火了！", "你会付出代价的！", "发生错误！痛死我了！", "嗷呜！" };
                other.ChatText = reactBarks[Rand.Next(reactBarks.Length)];
                other.ChatTimer = 120;
                
                // 被攻击者开始追逐
                other.ChasingTarget = this;
                other.ChaseTimer = 450; 
            }
            TargetRobot = null;
        });
    }

    private async void StartFight(Robot other)
    {
        if (FightTarget != null || other.FightTarget != null) return;

        // 初始化5轮对骂
        FightTarget = other;
        other.FightTarget = this;
        FightRounds = 5;
        other.FightRounds = 5;
        
        SocialCooldown = 800;
        other.SocialCooldown = 800;
        PauseTimer = 450;
        other.PauseTimer = 450;

        string insult = $"{other.Name}，你这没用的铁皮疙瘩，别挡我的路！";
        await SpeakInsult(insult, other);
    }

    public async Task SpeakInsult(string text, Robot target)
    {
        ChatText = text;
        ChatTimer = 120; // 稍长一点方便阅读
        SpecialState = "ANGRY";
        SpecialStateTimer = 120;
        
        LogSocial(Name, $"[实名辱骂] {text}");

        // 直接触发对方的接收逻辑 (带一点反应延迟)
        _ = Task.Delay(2000).ContinueWith(_ => {
            if (target.IsActive) target.ReceiveFightMessageAsync(Name, text, this);
        });
    }

    public async Task ReceiveFightMessageAsync(string senderName, string message, Robot sender)
    {
        if (!IsActive || FightRounds <= 0) return;

        // 吵架内容发送到世界频道
        TerminalManagerForm.Instance.BroadcastToWorld(senderName, $"💥 {message}", Color.OrangeRed);
        
        LogSocial(senderName, $"[被点名] {message}");
        
        // 表现受打击或愤怒
        SpecialState = "SHAKING";
        SpecialStateTimer = 80;
        
        // 思考后实名反击
        _isThinking = true;
        await Task.Delay(1500 + Rand.Next(1000));
        _isThinking = false;
        
        if (!IsActive) return;

        FightHistory.Add((senderName, message));
        if (FightHistory.Count > 10) FightHistory.RemoveAt(0);

        var rebut = await AiService.GetFightResponseAsync(Name, Personality, message, FightHistory, senderName);
        
        FightRounds--;
        if (FightRounds > 0 && sender.IsActive)
        {
            await SpeakInsult(rebut, sender);
        }
        else
        {
            ChatText = $"{senderName}，懒得理你了！";
            ChatTimer = 120;
            FightTarget = null;
        }
    }

    private async void TriggerSocialMeeting(Robot other)
    {
        if (MeetingTarget != null || other.MeetingTarget != null) return;
        
        MeetingTarget = other;
        other.MeetingTarget = this;
        MeetingTimer = 300;
        other.MeetingTimer = 300;
        SocialCooldown = 600;
        other.SocialCooldown = 600;
        
        PauseTimer = 180;
        other.PauseTimer = 180;

        string initialMsg = $"你好啊，{other.Name}！";
        // 只有 50% 概率发起真正的 AI 开场白
        if (Rand.Next(2) == 0)
        {
            var result = await AiService.GetSocialResponseAsync(Name, Personality, $"对{other.Name}打个招呼", new List<(string sender, string content)>(), other.Name, other.Personality);
            initialMsg = result;
        }

        ChatText = initialMsg;
        ChatTimer = 120;
        LogSocial(Name, initialMsg);
        
        await Task.Delay(2000);
        if (other.IsActive) await other.ReceiveSocialMessageAsync(Name, initialMsg, this);
    }

    public async Task ReceiveSocialMessageAsync(string senderName, string message, Robot sender)
    {
        if (!IsActive) return;
        
        LogSocial(senderName, message);
        
        // 思考一会
        _isThinking = true;
        await Task.Delay(1500);
        _isThinking = false;

        var history = SocialHistory.TakeLast(6).ToList();
        var response = await AiService.GetSocialResponseAsync(Name, Personality, message, history, senderName, sender.Personality);
        
        ChatText = response;
        ChatTimer = 150;
        LogSocial(Name, response);

        // 如果对方还在等，继续回聊 (限制对话长度以免陷入死循环)
        if (MeetingTimer > 100 && Rand.Next(100) < 70) 
        {
            await Task.Delay(3000);
            if (sender.IsActive) await sender.ReceiveSocialMessageAsync(Name, response, this);
        }
    }

    private void LogSocial(string sender, string content)
    {
        string log = $"[SOCIAL] {sender}: {content}";
        SocialHistory.Add((sender, content));
        if (SocialHistory.Count > 20) SocialHistory.RemoveAt(0);
        
        if (LogSocialInteractions)
        {
            NotifyOutput(log);
            // 同时也发送到世界大厅
            Color chatColor = sender == Name ? PrimaryColor : Color.SkyBlue;
            TerminalManagerForm.Instance.BroadcastToWorld(sender, content, chatColor);
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
