# 🐙 Octopus (CockroachPet) - 像素机器人桌面宠物

一个功能丰富的桌面宠物应用，可爱的像素风格八爪鱼机器人在你的桌面上自由移动、战斗，并且每个机器人都有自己的CMD终端！

## ✨ 核心特性

### 🤖 桌面机器人系统
- 像素艺术风格的八爪鱼机器人
- 自主移动、随机行为、边界反弹
- 动画效果：眨眼、触手摆动、天线晃动
- 6种随机颜色主题
- 可自定义名字、大小、速度
- **AI自主思考系统**：机器人可以自主思考并表达想法
- **性格系统**：支持多种性格类型（友好、害羞、攻击性等）

### ⚔️ 战斗系统
- **大乱斗模式**：机器人之间的实时格斗战斗
- **战略分期机制**：根据存活人数自动切换战斗策略
  - 乱战阶段（奇数存活）：远程打击，保持距离
  - 决战阶段（偶数存活）：近身格斗，宿命对决
- **陀螺对冲格斗**：炫酷的近身碰撞特效
- **技能系统**：火箭、激光、雷电等技能
- **怪物系统**：可投放怪物参与战斗
- **胜者进化**：获胜者吞噬对手，体型永久增长
- **反消极机制**：防止机器人"划水"

### 💻 集成终端
- 每个机器人都有独立的CMD终端
- 支持所有Windows CMD命令
- 点击机器人打开/显示终端
- 终端窗口关闭后后台继续运行
- 终端管理器：可视化管理所有终端窗口
- 世界聊天功能：机器人之间的通信

### 🎛️ 控制面板
- 可视化管理所有机器人
- 实时显示机器人状态
- 终端管理（显示/隐藏/关闭）
- 全局控制（暂停/启动/清除）
- 技能管理和释放

### 🐟 摸鱼模式
- **Ctrl+Shift+M** 快捷键开启
- 多种伪装主题：Excel表格、VS Code编辑器、CMD终端、Word文档
- 一键切换，完美伪装工作界面
- 支持切换不同摸鱼主题

### ⚙️ 其他功能
- 系统托盘图标管理
- 右键菜单快速操作
- 速度调节（50%-300%）
- 点击穿透模式（F11）
- 透明度调节
- 设置持久化
- 音效系统
- 错误日志自动记录

## 🚀 快速开始

### 运行要求
- Windows 10/11
- .NET 8.0 Runtime

### 编译运行
```bash
dotnet build
dotnet run
```

或直接运行编译后的 `CockroachPet.exe`

## 🎮 使用方法

### 基本操作
- **左键点击机器人** - 打开该机器人的CMD终端
- **右键托盘图标** - 打开菜单
- **ESC键** - 打开菜单
- **F11键** - 切换点击穿透模式
- **空格键** - 暂停/继续所有机器人
- **Ctrl+Shift+M** - 开启/关闭摸鱼模式
- **Ctrl+Shift+B** - 切换摸鱼主题
- **Ctrl+Shift+S** - 投放怪物
- **Ctrl+Shift+↑/↓** - 调节透明度

### 控制面板
1. 右键托盘图标 → "打开控制面板"
2. 双击机器人 → 打开/显示终端
3. 右键机器人 → 更多操作
4. 管理技能释放和战斗设置

### 终端操作
- 在CMD中可以执行所有Windows命令
- 点击窗口X按钮 → 隐藏到后台
- 输入 `exit` → 真正关闭终端
- 机器人命令：
  - `robot-name` - 显示名字
  - `robot-status` - 显示状态
  - `robot-resume` - 恢复移动
  - `robot-stop` - 停止移动

### 战斗系统
1. 通过控制面板或快捷键开启战斗模式
2. 机器人将自动进入战斗状态
3. 观看机器人之间的格斗表演
4. 获胜者将吞噬对手并进化

## 📁 项目结构

```
CockroachPet/
├── Core/
│   ├── Program.cs              # 程序入口
│   ├── ConPtyTerminal.cs       # 终端管理
│   ├── EmbeddedTerminal.cs     # 嵌入式终端
│   └── SelfImprovingManager.cs # 自我改进管理
├── UI/
│   ├── Form1.cs                # 主窗口和核心逻辑
│   ├── Form1.Designer.cs       # 主窗口设计器
│   ├── ControlPanelForm.cs     # 控制面板
│   ├── SettingsForm.cs         # 设置对话框
│   └── TerminalManagerForm.cs  # 终端管理器
├── Models/
│   ├── Robot.cs                # 机器人实体类
│   ├── Robot.AI.cs             # 机器人AI系统
│   ├── Robot.Animation.cs      # 机器人动画
│   ├── Robot.Combat.cs         # 机器人战斗
│   ├── Robot.Emotion.cs        # 机器人情感
│   ├── Robot.Personality.cs    # 机器人性格
│   ├── Robot.Physics.cs        # 机器人物理
│   ├── Robot.Skills.cs         # 机器人技能
│   ├── Robot.Social.cs         # 机器人社交
│   ├── Monster.cs              # 怪物类
│   ├── Projectile.cs           # 投射物类
│   ├── Skill.cs                # 技能类
│   └── BossModeTheme.cs        # 摸鱼模式主题
├── Rendering/
│   ├── PixelRobotRenderer.cs   # 像素风格渲染器
│   └── MonsterRenderer.cs      # 怪物渲染器
├── Services/
│   ├── AiService.cs            # AI服务
│   ├── AudioManager.cs         # 音频管理
│   ├── PersistenceManager.cs   # 持久化管理
│   └── SkillManager.cs         # 技能管理
├── Assets/
│   └── roach.png               # 资源文件
├── Skills/                     # 技能资源
└── CockroachPet.csproj         # 项目文件
```

## 🛠️ 技术栈

- **框架**: .NET 8.0 Windows Forms
- **图形渲染**: GDI+
- **窗口控制**: Win32 API (P/Invoke)
- **动画系统**: 定时器驱动的帧动画
- **战斗系统**: 实时物理碰撞检测
- **AI系统**: 基于状态机的自主行为
- **终端集成**: 进程管道重定向
- **配置管理**: JSON持久化存储

## 🎯 核心功能实现

### 像素渲染系统
- 使用GDI+绘制4px像素块
- 支持0.5x到3.0x平滑缩放
- 双缓冲技术减少闪烁
- 6种预设调色盘

### 窗口控制技术
- 点击穿透：`WS_EX_TRANSPARENT | WS_EX_LAYERED`
- 透明处理：`UpdateLayeredWindow`
- 层级管理：`HWND_TOPMOST`
- 窗口嵌入：`SetWindowLong`, `SetParent`

### 战斗系统架构
- 基于向量反射的碰撞检测
- 实时伤害计算和死亡判定
- 动态AI行为切换
- 视觉特效和音效反馈

### 终端管理
- Windows CMD进程管理
- 命令管道重定向
- 窗口Hook拦截关闭消息
- 后台进程持久化

## 📝 开发说明

### 构建项目
```bash
dotnet build
```

### 发布为单文件
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### 调试运行
```bash
dotnet run --configuration Debug
```

## 🐛 常见问题

1. **机器人不显示**
   - 确保.NET 8.0 Runtime已安装
   - 检查是否有其他窗口遮挡
   - 尝试调整透明度

2. **终端无法打开**
   - 检查系统CMD是否正常
   - 查看错误日志：`%TEMP%\CockroachPet_Error.log`

3. **战斗系统卡顿**
   - 降低机器人数量
   - 调整游戏速度设置

## 📄 许可证

MIT License

## 🤝 贡献

欢迎提交Issue和Pull Request！

## 📧 联系方式

如有问题或建议，请通过GitHub Issues联系。

---

Made with ❤️ by Kiro AI Assistant