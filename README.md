# 🐙 Octopus - Pixel Robot Pet

一个有趣的桌面宠物应用，可爱的像素风格八爪鱼机器人在你的桌面上自由移动，并且每个机器人都有自己的CMD终端！

## ✨ 特性

### 🤖 桌面机器人
- 像素艺术风格的八爪鱼机器人
- 自主移动、随机行为、边界反弹
- 动画效果：眨眼、触手摆动、天线晃动
- 6种随机颜色主题
- 可自定义名字、大小、速度

### 💻 集成终端
- 每个机器人都有独立的CMD终端
- 支持所有Windows CMD命令
- 点击机器人打开/显示终端
- 关闭窗口时终端在后台继续运行
- 自定义机器人命令（robot-name, robot-status等）

### 🎛️ 控制面板
- 可视化管理所有机器人
- 实时显示机器人状态
- 终端管理（显示/隐藏/关闭）
- 全局控制（暂停/启动/清除）

### ⚙️ 其他功能
- 只有一个主程序托盘图标
- 右键菜单快速操作
- 速度调节（50%-300%）
- 点击穿透模式
- 设置持久化

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

### 控制面板
1. 右键托盘图标 → "🎛️ 打开控制面板"
2. 双击机器人 → 打开/显示终端
3. 右键机器人 → 更多操作

### 终端操作
- 在CMD中可以执行所有Windows命令
- 点击窗口X按钮 → 隐藏到后台
- 输入 `exit` → 真正关闭终端
- 机器人命令：
  - `robot-name` - 显示名字
  - `robot-status` - 显示状态
  - `robot-resume` - 恢复移动
  - `robot-stop` - 停止移动

## 📁 项目结构

```
CockroachPet/
├── Program.cs              # 程序入口
├── Form1.cs                # 主窗口和核心逻辑
├── Robot.cs                # 机器人实体类
├── PixelRobotRenderer.cs   # 像素风格渲染器
├── TerminalForm.cs         # CMD终端管理
├── ControlPanelForm.cs     # 控制面板
├── SettingsForm.cs         # 设置对话框
└── Assets/
    └── roach.png           # 资源文件
```

## 🛠️ 技术栈

- .NET 8.0 Windows Forms
- GDI+ 图形渲染
- Win32 API（窗口控制）
- 定时器驱动的动画系统

## 📝 开发说明

### 核心功能实现
- **像素渲染**：使用GDI+绘制4px像素块
- **窗口Hook**：通过Win32 API拦截CMD窗口关闭消息
- **进程管理**：管理CMD进程的生命周期
- **状态同步**：实时更新机器人和终端状态

## 📄 许可证

MIT License

## 🤝 贡献

欢迎提交Issue和Pull Request！

## 📧 联系方式

如有问题或建议，请通过GitHub Issues联系。

---

Made with ❤️ by Kiro AI Assistant
