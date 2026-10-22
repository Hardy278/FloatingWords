# FloatingWords · 词浮

> 一张悬浮在桌面上的词汇卡片。

![Status](https://img.shields.io/badge/status-pre--alpha-orange)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-Windows-0078D4)](#环境要求)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

FloatingWords（中文名“词浮”）是一个面向 Windows 的桌面词汇曝光工具。

它以无边框、置顶、半透明有色磨砂玻璃卡片的形式常驻桌面。用户点击“开始吧”后，卡片按照设定时间自动轮换词义；在默认的 **Flash 定时闪卡**模式下，评级只改变未来出现权重，不会打断当前展示节奏。

## 项目状态

当前项目处于**早期骨架阶段**：

- ✅ WPF 项目骨架
- ✅ 开源仓库基础设施（README、License、CI、Issue/PR 模板、EditorConfig）
- ⏳ 领域模型与选词调度
- ⏳ 玻璃悬浮窗与开始/结束流程
- ⏳ 词书导入、SQLite 状态与设置界面

现阶段应用还不能作为完整产品使用。实际功能会在后续开发中逐步实现。

## 目标功能

- **Flash 定时闪卡**：按用户设置的秒数自动轮换义项，默认 5 秒。
- **悬停暂停**：Flash 模式可选；移入窗口冻结倒计时，移出后从剩余时间继续。
- **点击优先模式**：点击评级立即换词，同时保留自动轮换作为兜底。
- **四层评级**：忘了、有点印象、会了、很简单；权重决定曝光概率。
- **无通知打扰**：不是 Toast 通知应用，而是一张持续存在的桌面词卡。
- **开始与结束**：每次启动先进入开始页；学习中可随时结束并回到开始页。
- **词书管理**：在设置中导入、切换、删除和刷新 JSON 词书。
- **按 role 定制样式**：词头、音标、词性、释义、例句等分别配置字体与颜色。
- **状态持久化**：评级写入 SQLite，设置和窗口位置写入 JSON。
- **有色磨砂玻璃视觉**：纯 WPF 实现的半透明玻璃质感，不依赖系统 Acrylic。

## 技术栈

- C# / .NET 10
- WPF（`net10.0-windows`）
- `System.Text.Json`
- `Microsoft.Data.Sqlite`
- xUnit（领域层测试）
- 原生 WPF 控件，不引入第三方 UI 框架

项目目录保持根级扁平结构，方便 WPF 工具链、Visual Studio、Rider 和 `dotnet` 命令直接使用；测试统一放在 `tests/` 下。

## 本地开发

### 环境要求

- Windows 10 或 Windows 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- 支持 .NET 10 的 IDE（Visual Studio、JetBrains Rider 或 VS Code）
- 可选：用于调试 WPF 的 Windows 桌面开发组件

### 构建与运行

```powershell
dotnet restore FloatingWords.slnx
dotnet build FloatingWords.slnx
dotnet run --project FloatingWords/FloatingWords.csproj
```

> 当前 `MainWindow` 仍是模板窗口；界面实现会随功能开发逐步替换。

### 测试

测试项目加入后：

```powershell
dotnet test FloatingWords.slnx
```

## 应用数据

正式版本计划使用：

```text
%AppData%\FloatingWords\
├─ books\
├─ settings.json
├─ state.db
└─ logs\
```

开发阶段不会把用户数据写入仓库。请不要提交任何本地词书、数据库、日志或 AppData 内容。

## 参与贡献

欢迎提交 Issue 和 Pull Request。大型功能、架构调整或交互变化，建议先通过 Issue 讨论，避免与当前项目目标冲突。请保持友善、具体的沟通方式。

## 许可证

本项目使用 [MIT License](LICENSE)。

## 致谢

- 使用 .NET、WPF、SQLite 与开源社区的工具构建。

---

**FloatingWords**：让词汇安静地浮在眼前。
