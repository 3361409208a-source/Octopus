using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

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

    // 生命值系统
    public int HP { get; set; } = 1000;
    public int MaxHP { get; set; } = 1000;
    public int DamageFeedbackTimer { get; set; } = 0; // 受击红闪透明度控制
    public string LastDamageText { get; set; } = ""; // 漂浮文字内容
    public int DamageTextTimer { get; set; } = 0; // 漂浮文字计时

    // 终端状态
    public string LastOutput { get; set; } = "";
    public string StatusMessage { get; set; } = "IDLE";
    public string AlertMessage { get; set; } = "";
    public bool IsWarning { get; set; } = false;
    public int WarningTimer { get; set; } = 0;
    public bool IsDead { get; set; } = false; // 死亡状态
    public int OriginalSize { get; set; } = 64; // 记录原始大小用于恢复
    public event Action<string>? OnTerminalOutput;
    
    // AI 自主思考设置
    public bool EnableAiThinking { get; set; } = false;
    public int AiThoughtFrequency { get; set; } = 60; // 单位：秒
    public int FightFrequency { get; set; } = 15; // 默认 15% 几率打架
    public bool IsWeaponMaster { get; set; } = false; // 武器大师模式
    

    
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

    // 物理互动 (扒拉、拉取、抛投)
    public Robot? PhysicalTarget { get; set; }
    public string PhysicalAction { get; set; } = "NONE"; // NONE, PUSH, PULL, GRAB, THROW
    public int PhysicalTimer { get; set; } = 0;
    public Robot? PullingMe { get; set; }
    public bool IsBeingThrown { get; set; } = false;

    // 社交交互
    public string ChatText { get; set; } = "";
    private string _fullChatText = "";
    private int _streamCounter = 0;
    public int ChatTimer { get; set; } = 0;
    public List<(string role, string content)> ChatHistory { get; set; } = new();
    public List<(string sender, string content)> SocialHistory { get; set; } = new();
    public bool IsAiSpeaking { get; set; } = false; // 标记是否正在进行正式AI对话
    public bool LogSocialInteractions { get; set; } = true;
    public Robot? MeetingTarget { get; set; }
    public int MeetingTimer { get; set; } = 0;
    
    // 吵架对骂系统
    public int SocialCooldown { get; set; } = 0;
    public Robot? FollowingTarget { get; set; }
    public int FollowTimer { get; set; } = 0;

    // 陀螺格斗模式
    public Robot? DuelTarget { get; set; }
    public int DuelTimer { get; set; } = 0;
    private float _duelAngle = 0;

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
    
    // 是否忙碌（正在进行互动/动画/AI思考）
    public bool IsBusy => IsDead || PhysicalAction != "NONE" || PullingMe != null || _isThinking || IsBeingThrown;

    // 自定义词库 (用户设置)
    public List<string> CustomPhrases { get; set; } = new List<string>();
    private int _customPhraseTimer = 300; // 约 10s 检查一次台词触发
    public int AggressionTimer { get; set; } = 0; // 反“渔翁得利”计时器

    // 随机行为
    public int PauseTimer { get; set; } = 0;
    public int StunTimer { get; set; } = 0; // 电击麻痹计时器
    public int SlowTimer { get; set; } = 0; // 减速计时器
    public int BlindTimer { get; set; } = 0; // 致盲计时器
    private Robot? _delayedAttackTarget = null;
    private int _delayedAttackTimer = 0;
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
        OriginalSize = Size;
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
        // 1. 基础物理边界限制 (确保所有状态都不越界)
        if (X < 0) { X = 0; Dx = -Dx; }
        if (X > screenWidth - Size) { X = screenWidth - Size; Dx = -Dx; }
        if (Y < 0) { Y = 0; Dy = -Dy; }
        if (Y > screenHeight - Size) { Y = screenHeight - Size; Dy = -Dy; }

        // 如果游戏进入终局（胜者吞噬阶段），仅执行基础位移
        if (Form1.Instance != null && Form1.Instance.IsGameEnding)
        {
            if (IsDead) { Dx = 0; Dy = 0; return; } // 尸体禁止乱动
            X += Dx;
            Y += Dy;
            // 终局下依然应用摩擦力
            Dx *= 0.98f;
            Dy *= 0.98f;
            return;
        }

        if (!IsActive) return;
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
                    string[] rages = { "嗷呜！", "受死吧！", "站住别跑！", "我要拆了你的主板！", "吃我一招！" };
                    SetBark(rages[Rand.Next(rages.Length)], 60);
                }
            }
            else
            {
                // 追上了，进入格斗模式
                StartDuel(ChasingTarget);
                ChaseTimer = 0;
                ChasingTarget = null;
            }
            
            ChaseTimer--;
            if (ChaseTimer == 0) ChasingTarget = null;
        }

        if (IsFiringLaser && TargetRobot != null && TargetRobot.IsActive)
        {
            LaserTargetX = TargetRobot.X + (float)TargetRobot.Size / 2;
            LaserTargetY = TargetRobot.Y + (float)TargetRobot.Size / 2;
        }
        else if (IsFiringLaser)
        {
            // 如果目标丢失或死亡，立即停止开火
            IsFiringLaser = false;
            TargetRobot = null;
        }

        // 被拉取/被抛投的物理限制
        if (PullingMe != null)
        {
            if (!PullingMe.IsActive || PullingMe.PhysicalTarget != this)
            {
                PullingMe = null;
            }
            else
            {
                // 强制同步位置到拉取者的触手位置（由于还没有具体的触手绘制逻辑，先拉到中心）
                float tx = PullingMe.X + PullingMe.Size / 2 - Size / 2;
                float ty = PullingMe.Y + PullingMe.Size / 2 - Size / 2;
                X = X * 0.7f + tx * 0.3f;
                Y = Y * 0.7f + ty * 0.3f;
            }
        }

        if (IsBeingThrown)
        {
            RotationAngle += 15;
            if (X <= 0 || Y <= 0 || X >= screenWidth - Size || Y >= screenHeight - Size)
            {
                IsBeingThrown = false;
                RotationAngle = 0;
            }
            
            // 扔出去的阻力
            Dx *= 0.98f;
            Dy *= 0.98f;
            if (Math.Abs(Dx) < 1 && Math.Abs(Dy) < 1) 
            {
                IsBeingThrown = false;
                RotationAngle = 0;
            }
        }

        if (PhysicalTimer > 0) PhysicalTimer--;
        if (StunTimer > 0) StunTimer--;
        if (SlowTimer > 0) SlowTimer--;
        if (BlindTimer > 0) BlindTimer--;

        if (PhysicalTimer == 0 && PhysicalAction != "NONE")
        {
            if (PhysicalAction == "GRAB") PerformThrow();
            PhysicalAction = "NONE"; 
            PhysicalTarget = null;
        }

        if (BlindTimer == 1) StatusMessage = "IDLE"; // 恢复状态

        // 处理延迟攻击反馈
        if (_delayedAttackTimer > 0)
        {
            _delayedAttackTimer--;
            if (_delayedAttackTimer == 0 && _delayedAttackTarget != null)
            {
                IsFiringLaser = false;
                if (_delayedAttackTarget.IsActive && !_delayedAttackTarget.IsDead)
                {
                    _delayedAttackTarget.ApplyAttackEffect();
                    _delayedAttackTarget.ChasingTarget = this;
                    _delayedAttackTarget.ChaseTimer = 450;
                }
                _delayedAttackTarget = null;
                TargetRobot = null;
            }
        }

        if (!IsMoving && ChaseTimer <= 0) return;

        // 停顿逻辑
        if (PauseTimer > 0)
        {
            PauseTimer--;
            UpdateTentacles(true); // 停顿时的触手动画
            return;
        }

        if (Rand.Next(1000) < 3)
        {
            PauseTimer = Rand.Next(30, 90);
            return;
        }

        // 陀螺格斗逻辑 (究极重构：对冲碰撞版)
        if (DuelTimer > 0 && DuelTarget != null && DuelTarget.IsActive)
        {
            if (DuelTarget.IsDead)
            {
                DuelTimer = 0;
                DuelTarget = null;
                RotationAngle = 0;
                return;
            }
            DuelTimer--;
            
            // 1. 寻找格斗中心点
            float centerX = (X + DuelTarget.X) / 2;
            float centerY = (Y + DuelTarget.Y) / 2;
            
            // 2. 格斗节奏控制 (每 40 帧一个冲锋轮回)
            int phase = DuelTimer % 40;
            float radius = 0;
            
            if (phase > 25) // 阶段 A：蓄力/后撤 (40 -> 26)
            {
                // 缓慢拉开距离，准备下一次冲击
                radius = 40 + (40 - phase) * 4; 
                SpecialState = "NORMAL";
            }
            else if (phase > 10) // 阶段 B：极速对冲 (25 -> 11)
            {
                // 距离瞬间归零，模拟撞击
                radius = (phase - 10) * 5; 
                SpecialState = "ANGRY";
                if (phase == 15) // 近战爆发台词
                {
                    string[] clashBarks = { "吃我一记冲撞！", "💥 像素碎裂！", "给老子死！", "铁拳制裁！" };
                    SetBark(clashBarks[Rand.Next(clashBarks.Length)], 30);
                }
            }
            else // 阶段 C：肉搏碰撞瞬间 (10 -> 0)
            {
                // 贴脸剧烈磨蹭，疯狂开火
                radius = (float)Rand.NextDouble() * 8; 
                SpecialState = "SHAKING";
                SpecialStateTimer = 2;
                RotationAngle += 60f; // 极速旋转
                
                // 碰撞瞬间极致开火 (频率略微降级，防止瞬间蒸发导致总是同归于尽)
                if (phase % 4 == 0) LaunchRemoteAttack(DuelTarget);
                
                // 屏幕抖动感位移
                X += (float)(Rand.NextDouble() - 0.5) * 15;
                Y += (float)(Rand.NextDouble() - 0.5) * 15;
            }
            
            // 3. 应用位置逻辑
            float dirX = (Id % 2 == 0) ? 1 : -1;
            float angle = (float)(Math.PI / 4 + (Id % 2) * Math.PI); // 保持对冲轴
            
            // 增加一点旋转偏转，让格斗轴线随时间转动，不至于死板
            float globalRotate = (DuelTimer / 100f) * (float)Math.PI;
            float finalAngle = angle + globalRotate;
            
            X = centerX + (float)Math.Cos(finalAngle) * radius;
            Y = centerY + (float)Math.Sin(finalAngle) * radius;

            if (DuelTimer == 0)
            {
                DuelTarget = null;
                // 最后的强力终结弹开
                float escapeX = X - centerX;
                float escapeY = Y - centerY;
                float eDist = (float)Math.Max(1, Math.Sqrt(escapeX * escapeX + escapeY * escapeY));
                Dx = (escapeX / eDist) * 30;
                Dy = (escapeY / eDist) * 30;
                SetBark("像素核心...爆发！🔥", 100);
            }
            return; // 模式锁定
        }

        // 追逐逻辑与边追边射
        if (ChaseTimer > 0 && ChasingTarget != null)
        {
            if (ChasingTarget.IsDead)
            {
                ChaseTimer = 0;
                ChasingTarget = null;
                return;
            }
            ChaseTimer--;
            float tx = ChasingTarget.X - X;
            float ty = ChasingTarget.Y - Y;
            float tdist = (float)Math.Sqrt(tx * tx + ty * ty);
            
            if (tdist > 40) // 追逐中：补全位移逻辑
            {
                Dx = (Dx * 0.9f) + (tx / tdist * 0.5f * SpeedMultiplier);
                Dy = (Dy * 0.9f) + (ty / tdist * 0.5f * SpeedMultiplier);
                
                // 边追边射
                if (ShootCooldown == 0 && Rand.Next(100) < 5) LaunchRemoteAttack(ChasingTarget);
            }
            else // 真正追上了
            {
                int aliveCount = Form1.Instance?.GetRobots().Count(r => !r.IsDead && r.IsVisible && r.IsActive) ?? 0;
                // 严格遵循：偶数格斗，奇数射击
                if (aliveCount > 0 && aliveCount % 2 == 0)
                {
                    StartDuel(ChasingTarget);
                }
                else
                {
                    if (ShootCooldown == 0) LaunchRemoteAttack(ChasingTarget);
                    PerformPush(ChasingTarget);
                }
                ChaseTimer = 0;
                ChasingTarget = null;
            }
            
            if (ChaseTimer == 0) ChasingTarget = null;
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

        // 移动与摩擦力
        float finalSpeed = SpeedMultiplier;
        if (SlowTimer > 0) finalSpeed *= 0.4f;
        if (StunTimer > 0) finalSpeed = 0; // 麻痹不能移动

        X += Dx * finalSpeed;
        Y += Dy * finalSpeed;
        
        // 全局摩擦力：使爆发速度逐渐平稳
        Dx *= 0.98f;
        Dy *= 0.98f;

        // 边界反馈已移至开头
        FacingRight = Dx >= 0;

        // 社交与跟随逻辑
        if (FollowTimer > 0 && FollowingTarget != null && FollowingTarget.IsActive)
        {
            if (FollowingTarget.IsDead)
            {
                FollowTimer = 0;
                FollowingTarget = null;
            }
            else
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
        }

        if (MeetingTimer > 0)
        {
            MeetingTimer--;
            if (MeetingTimer == 0) MeetingTarget = null;
        }

        if (SocialCooldown > 0) SocialCooldown--;

        if (SocialCooldown > 0) SocialCooldown--;
        if (ChatTimer > 0) ChatTimer--;
        if (DamageTextTimer > 0) DamageTextTimer--;
        if (DamageFeedbackTimer > 0) DamageFeedbackTimer--;
        
        // 自动回血逻辑 (非格斗模式且未死亡下)
        if (DuelTimer == 0 && DuelTarget == null && !IsDead && HP < MaxHP && Rand.Next(100) < 5) 
        {
            HP = Math.Min(MaxHP, HP + 1);
        }

        // 生命耗尽处理
        if (HP <= 0 && !IsDead)
        {
            IsDead = true;
            IsMoving = false;
            SpecialState = "NORMAL";
            RotationAngle = 90f; // 侧倒死亡
            SetBark("核心崩溃...系统下线 💀", 200);
            Dx = 0; Dy = 0;
        }

        if (IsDead) 
        {
            Dx = 0; Dy = 0; // 彻底锁死位移
            // 即使死亡，也要保持位置修正
            X = Math.Max(0, Math.Min(X, screenWidth - Size));
            Y = Math.Max(0, Math.Min(Y, screenHeight - Size));
            return; 
        }

        // 流式文字逻辑
        UpdateStreamingChat();

        // 自定义台词随机触发（纯文字，与语音分离）
        UpdateCustomPhrases();



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
        else 
        {
            // 反“渔翁得利”逻辑：如果长期处于空闲状态，强制寻找目标
            if (DuelTimer == 0 && ChaseTimer == 0 && FollowingTarget == null && !IsDead)
            {
                AggressionTimer++;
                if (AggressionTimer > 600) // 约 10 秒没动静
                {
                    var targets = Form1.Instance?.GetRobots()
                                   .Where(r => r != this && !r.IsDead && r.IsVisible)
                                   .OrderByDescending(r => r.HP)
                                   .ToList();
                    if (targets != null && targets.Count > 0)
                    {
                        ChasingTarget = targets[0];
                        ChaseTimer = 300;
                        SetBark($"{ChasingTarget.Name}，别在那划水了！过两招！💢", 100);
                        AggressionTimer = 0;
                    }
                }
            }
            else
            {
                AggressionTimer = 0;
            }

            if (Rand.Next(1000) < 5) // 0.5% 概率触发随机动画
            {
                string[] states = { "HEART_EYES", "SPINNING", "BLUSH", "SLEEPY", "ANGRY" };
                SpecialState = states[Rand.Next(states.Length)];
                SpecialStateTimer = Rand.Next(60, 180);
            }
        }

        // 武器大师视觉增强：保持愤怒眼神
        if (IsWeaponMaster && SpecialState == "NORMAL")
        {
            SpecialState = "ANGRY";
            SpecialStateTimer = 60;
        }

        // 表情气泡逻辑
        if (EmojiBubbleTimer > 0)
        {
            EmojiBubbleTimer--;
        }
        else if (!IsAiSpeaking && Rand.Next(2000) < 3)
        {
            string[] emojis = { "☕", "💡", "🎮", "🎵", "🍕", "⭐", "🔥", "💨" };
            CurrentEmoji = emojis[Rand.Next(emojis.Length)];
            EmojiBubbleTimer = Rand.Next(60, 120);
        }
    }

    private void UpdateCustomPhrases()
    {
        // 纯台词系统：与语音完全分离，独立随机触发
        if (CustomPhrases.Count == 0 || !IsActive || IsBusy) return;

        if (_customPhraseTimer > 0)
        {
            _customPhraseTimer--;
        }
        else
        {
            // 30% 概率触发台词（只显示文字，不播放语音）
            if (Rand.Next(100) < 30)
            {
                string phrase = CustomPhrases[Rand.Next(CustomPhrases.Count)];
                SetBark(phrase, 120);
                Console.WriteLine($"[CustomPhrase] {Name} 说: {phrase}");
            }
            _customPhraseTimer = Rand.Next(180, 600); // 6s ~ 20s 随机间隔
        }
    }



    private void UpdateAiThinking()
    {
        if (_isThinking || !IsActive || !EnableAiThinking) return;

        if (AiThoughtTimer > 0)
        {
            AiThoughtTimer--;
        }
        else
        {
            // 将频率转换为帧数 (约 30fps)
            int targetFrames = AiThoughtFrequency * 30;
            // 增加一点随机抖动 (±20%)
            int jitter = (int)(targetFrames * 0.2);
            AiThoughtTimer = targetFrames + Rand.Next(-jitter, jitter);
            
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
            IsAiSpeaking = true;
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
        else if (IsAiSpeaking && ChatTimer == 0)
        {
            IsAiSpeaking = false; // 对话结束
        }
    }

    public void SetBark(string text, int duration = 90)
    {
        // 如果正在进行正式AI对话，则忽略无关紧要的默认气泡
        if (IsAiSpeaking) return;
        
        _fullChatText = text;
        ChatText = text; // Bark 不需要流式
        ChatTimer = duration;
    }

    public async Task SendUserMessage(string message)
    {
        if (_isThinking) return;
        
        OnChatMessageReceived?.Invoke("user", message, "");
        ChatHistory.Add(("user", message));
        if (ChatHistory.Count > 10) ChatHistory.RemoveAt(0);

        _isThinking = true;
        IsAiSpeaking = true;
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
        
        IsAiSpeaking = true;
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
            if (skillKeys.Count > 0)
            {
                var randomSkill = skillKeys[Rand.Next(skillKeys.Count)];
                Skills[randomSkill].GainExperience(50);
                
                // 物理保存技能文件
                SkillManager.SaveRobotSkills(this);
                System.Diagnostics.Debug.WriteLine($"[Robot {Name}] 进化成功！新感悟: {result.Insight}, 技能{randomSkill}+50XP");
            }
            
            _fullChatText = $"（记忆已更新：{result.Insight}）";
            ChatText = "";
            ChatTimer = 120; // 显示一两秒
            
            OnGrowthUpdated?.Invoke(this);
        }

        ChatTimer = 0; // 结束进化提示
    }

    public void InteractWith(Robot other)
    {
        if (SocialCooldown > 0 || other.SocialCooldown > 0) return;
        if (IsBusy || other.IsBusy) return;

        float dx = other.X - X;
        float dy = other.Y - Y;
        float dist = (float)Math.Sqrt(dx * dx + dy * dy);

        // 获取当前存活人数 (核心战略判断)
        int aliveCount = Form1.Instance?.GetRobots().Count(r => !r.IsDead && r.IsVisible && r.IsActive) ?? 0;

        if (aliveCount % 2 != 0)
        {
            // --- 方案 A：奇数存活 - 严格远程狙击模式 ---
            if (dist < 100)
            {
                // 奇数时严禁近战（防止死循环），靠得太近就强制推开并补枪
                PerformPush(other);
                SocialCooldown = 45;
                other.SocialCooldown = 45;
                if (ShootCooldown == 0) LaunchRemoteAttack(other);
                SetBark("人太多了，离我远点射击！", 60);
            }
            else if (dist < 600 && ShootCooldown == 0)
            {
                // 保持高频率远程互射
                int chance = IsWeaponMaster ? 40 : 20;
                if (Rand.Next(100) < chance)
                {
                    LaunchRemoteAttack(other);
                    SocialCooldown = 60;
                }
            }
        }
        else
        {
            // --- 方案 B：偶数存活 - 允许近身捉对厮杀 ---
            if (dist < 80)
            {
                // 偶数时触发像素格斗，寻找对手进行对冲
                StartDuel(other);
                SocialCooldown = 100;
                other.SocialCooldown = 100;
                SetBark(aliveCount == 2 ? "这是最后的清算！💥" : "找到对手了，来格斗吧！", 80);
            }
            else
            {
                // 偶数人数时更有侵略性地发起追逐，模拟“捉对”
                if (ChaseTimer <= 0 && FollowingTarget == null && Rand.Next(100) < 15)
                {
                    ChasingTarget = other;
                    ChaseTimer = 350;
                    SetBark($"锁定目标：{other.Name}！", 60);
                }
                
                // 追的过程中该射也得射
                if (dist > 150 && ShootCooldown == 0 && Rand.Next(100) < 15)
                {
                    LaunchRemoteAttack(other);
                    SocialCooldown = 30;
                }
            }
        }
    }

    private void LaunchRemoteAttack(Robot other)
    {
        if (other == null || !other.IsActive || other.IsDead || IsDead) return;

        string[] attackBarks = { "看招！炸裂吧！💥", "吃我一记像素光波！⚡", "系统过载灌入！🔥", "目标锁定，发射！🎯", "吃我一记禁言锤！🔨", "像素风暴攻击！🌀" };
        SetBark(attackBarks[Rand.Next(attackBarks.Length)], 100);
        SpecialState = "ANGRY";
        SpecialStateTimer = 100;
        ShootCooldown = IsWeaponMaster ? 120 : 400;

        if (IsWeaponMaster)
        {
            // 武器大师模式：发射多样化实体子弹
            string[] ammoTypes = { "BULLET", "ROCKET", "PLASMA", "CANNON", "LIGHTNING", "SPIT", "INK" };
            string selectedType = ammoTypes[Rand.Next(ammoTypes.Length)];
            
            // 针对特定武器的特殊台词
            string weaponBark = selectedType switch {
                "CANNON" => "超级重炮...发射！💣",
                "LIGHTNING" => "十万伏特！⚡",
                "SPIT" => "受死吧！呸！🤮",
                "INK" => "墨迹干扰！🕶️",
                "ROCKET" => "追踪火箭！🚀",
                _ => "接受像素打击吧！"
            };
            SetBark(weaponBark, 80);

            float centerX = X + Size / 2;
            float centerY = Y + Size / 2;
            float targetX = other.X + other.Size / 2;
            float targetY = other.Y + other.Size / 2;

            var p = new Projectile(this, centerX, centerY, targetX, targetY, selectedType, other);
            Form1.Instance?.AddProjectile(p);
        }
        else
        {
            // 普通模式：维持视觉特效攻击
            string[] types = { "LASER", "SHOCK", "BURST" };
            CurrentAttackType = types[Rand.Next(types.Length)];
            TargetRobot = other;
            IsFiringLaser = true;
            LaserTargetX = other.X + (float)other.Size / 2;
            LaserTargetY = other.Y + (float)other.Size / 2;

            _delayedAttackTarget = other;
            _delayedAttackTimer = 25; // 约 0.4s
        }
    }

    public void ApplyAttackEffect(int damage = 5)
    {
        if (IsDead) return;
        HP = Math.Max(0, HP - damage);
        
        if (HP <= 0)
        {
            IsDead = true;
            IsMoving = false;
            RotationAngle = 90f; // 立即倒地
            Dx = 0; Dy = 0;
            SetBark("核心崩溃...系统下线 💀", 200);
        }

        LastDamageText = $"-{damage}";
        DamageTextTimer = 45; // 约 0.7s
        DamageFeedbackTimer = 60; // 红闪

        SpecialState = "SHAKING";
        SpecialStateTimer = 60;
        string[] reactBarks = { "哎哟！谁偷袭我？！", "我的电路着火了！", "你会付出代价的！", "发生错误！痛死我了！", "嗷呜！" };
        if (Rand.Next(100) < 30) SetBark(reactBarks[Rand.Next(reactBarks.Length)], 120);
    }

    public void HandleProjectileHit(Projectile p)
    {
        if (!IsActive) return;
        
        // 根据伤害类型计算伤害
        int baseDmg = p.Type switch {
            "ROCKET" => 15,
            "CANNON" => 25,
            "LIGHTNING" => 10,
            "PLASMA" => 20,
            _ => 5
        };
        ApplyAttackEffect(baseDmg);
        
        // 根据子弹类型增加额外物理反馈
        switch(p.Type)
        {
            case "ROCKET":
                Dx += p.Dx * 0.8f;
                Dy += p.Dy * 0.8f;
                SpecialState = "SPINNING";
                SpecialStateTimer = 90;
                break;
            case "CANNON":
                Dx += p.Dx * 1.5f;
                Dy += p.Dy * 1.5f;
                SpecialState = "SHAKING";
                SpecialStateTimer = 150;
                SetBark("好重！像是被卡车撞了！", 60);
                break;
            case "LIGHTNING":
                StunTimer = 90; // 麻痹
                SpecialState = "SHAKING";
                SpecialStateTimer = 90;
                SetBark("⚡ 呃啊！被电焦了！", 60);
                break;
            case "SPIT":
                SlowTimer = 180; // 减速 3s
                SetBark("呸！太恶心了！🤮", 60);
                break;
            case "INK":
                BlindTimer = 240; // 致盲 4s
                StatusMessage = "谁在墨水里下毒了！";
                SetBark("黑漆漆的一片！🕶️", 60);
                break;
            case "PLASMA":
                SpecialState = "SPINNING";
                SpecialStateTimer = 120;
                break;
            case "BULLET":
            default:
                Dx += p.Dx * 0.2f;
                Dy += p.Dy * 0.2f;
                break;
        }

        // 建立仇恨
        if (p.Owner != null && p.Owner != this)
        {
            ChasingTarget = p.Owner;
            ChaseTimer = 400;
        }
    }

    private void PerformPush(Robot other)
    {
        SetBark("给我起开！", 60);
        PhysicalTarget = other;
        PhysicalAction = "PUSH";
        PhysicalTimer = 60;

        float ox = other.X - X;
        float oy = other.Y - Y;
        float odist = (float)Math.Max(1, Math.Sqrt(ox * ox + oy * oy));
        other.Dx = (ox / odist) * 12 * other.SpeedMultiplier;
        other.Dy = (oy / odist) * 12 * other.SpeedMultiplier;
        other.SpecialState = "SHAKING";
        other.SpecialStateTimer = 30;
    }

    private void PerformPull(Robot other)
    {
        SetBark("过来吧你！", 60);
        PhysicalTarget = other;
        PhysicalAction = "PULL";
        PhysicalTimer = 60;
        other.PullingMe = this;
    }

    private void PerformGrab(Robot other)
    {
        SetBark("抓到你了！", 40);
        PhysicalTarget = other;
        PhysicalAction = "GRAB";
        PhysicalTimer = 40; // 抓稳需要点时间
        other.PullingMe = this;
    }

    private void PerformThrow()
    {
        if (PhysicalTarget == null || !PhysicalTarget.IsActive) return;
        
        SetBark("走你！", 60);

        // 尝试寻找第三个幸运儿
        Robot? thirdObj = Form1.Instance.GetRobots()
            .Where(r => r != this && r != PhysicalTarget && r.IsVisible)
            .OrderBy(r => Math.Abs(r.X - X) + Math.Abs(r.Y - Y))
            .FirstOrDefault();

        float tx, ty;
        if (thirdObj != null)
        {
            tx = thirdObj.X - X;
            ty = thirdObj.Y - Y;
        }
        else
        {
            tx = Rand.Next(-200, 200);
            ty = Rand.Next(-200, 200);
        }

        float dist = (float)Math.Max(1, Math.Sqrt(tx * tx + ty * ty));
        PhysicalTarget.Dx = (tx / dist) * 25 * PhysicalTarget.SpeedMultiplier;
        PhysicalTarget.Dy = (ty / dist) * 25 * PhysicalTarget.SpeedMultiplier;
        PhysicalTarget.IsBeingThrown = true;
        PhysicalTarget.PullingMe = null;
        
        // 播报到世界频道
        TerminalManagerForm.Instance.BroadcastToWorld(Name, $"🤼 将 {PhysicalTarget.Name} 像垃圾一样扔了出去！", Color.Orange);
    }

    private void StartDuel(Robot other)
    {
        if (DuelTarget != null || other.DuelTarget != null) return;
        
        DuelTarget = other;
        other.DuelTarget = this;
        DuelTimer = 180; // 持续时间
        other.DuelTimer = 180;
        
        _duelAngle = (float)(Rand.NextDouble() * Math.PI * 2);
        other._duelAngle = _duelAngle + (float)Math.PI; // 对角位置

        string[] dBark = { "陀螺模式启动！", "看我旋风冲锋！", "格斗开始！", "像素激战！" };
        SetBark(dBark[Rand.Next(dBark.Length)], 60);
        other.SetBark(dBark[Rand.Next(dBark.Length)], 60);
        
        SpecialState = "SPINNING";
        other.SpecialState = "SPINNING";
    }

    private void LogSocial(string sender, string content, bool broadcast = true)
    {
        string log = $"[SOCIAL] {sender}: {content}";
        SocialHistory.Add((sender, content));
        if (SocialHistory.Count > 20) SocialHistory.RemoveAt(0);
        
        if (LogSocialInteractions)
        {
            NotifyOutput(log);
            
            if (broadcast)
            {
                // 同时也发送到世界大厅
                Color chatColor = sender == Name ? PrimaryColor : Color.SkyBlue;
                TerminalManagerForm.Instance.BroadcastToWorld(sender, content, chatColor);
            }
        }
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
