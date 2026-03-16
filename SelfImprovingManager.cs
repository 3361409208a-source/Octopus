using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace CockroachPet
{
    public class SelfImprovingManager
    {
        public string RobotName { get; }
        public int RobotId { get; }
        private string BasePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CockroachPet", "SelfImproving", $"{RobotId}_{RobotName}");

        private string _baseInstructions = "";

        public SelfImprovingManager(int robotId, string robotName)
        {
            RobotId = robotId;
            RobotName = robotName;
            LoadBaseInstructions();
            EnsureStructure();
        }

        private void LoadBaseInstructions()
        {
            try
            {
                // 尝试从项目目录加载下载好的技能定义
                string projectSkillPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Skills", "self-improving-1.2.16", "SKILL.md");
                // 备选路径（开发目录）
                if (!File.Exists(projectSkillPath))
                {
                    projectSkillPath = @"d:\xiangmu\sprite\CockroachPet\CockroachPet\Skills\self-improving-1.2.16\SKILL.md";
                }

                if (File.Exists(projectSkillPath))
                {
                    _baseInstructions = File.ReadAllText(projectSkillPath);
                    System.Diagnostics.Debug.WriteLine($"[SelfImproving] Loaded base instructions from {projectSkillPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SelfImproving] LoadBase Error: {ex.Message}");
            }
        }

        private void EnsureStructure()
        {
            try
            {
                string[] dirs = { "", "projects", "domains", "archive" };
                foreach (var dir in dirs)
                {
                    string path = Path.Combine(BasePath, dir);
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                }

                InitializeFile("memory.md", "# Memory (HOT Tier)\n\n## Preferences\n\n## Patterns\n\n## Rules\n");
                InitializeFile("corrections.md", "# Corrections Log\n\n| Date | What I Got Wrong | Correct Answer | Status |\n|------|-----------------|----------------|--------|\n");
                InitializeFile("index.md", "# Memory Index\n\n| File | Lines | Last Updated |\n|------|-------|--------------|\n| memory.md | 0 | — |\n| corrections.md | 0 | — |\n");
                InitializeFile("heartbeat-state.md", "# Self-Improving Heartbeat State\n\nlast_heartbeat_started_at: never\nlast_reviewed_change_at: never\nlast_heartbeat_result: never\n\n## Last actions\n- none yet\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SelfImproving] Init Error: {ex.Message}");
            }
        }

        private void InitializeFile(string fileName, string defaultContent)
        {
            string path = Path.Combine(BasePath, fileName);
            if (!File.Exists(path))
            {
                File.WriteAllText(path, defaultContent);
            }
        }

        public string GetHotMemory()
        {
            try
            {
                string path = Path.Combine(BasePath, "memory.md");
                if (File.Exists(path)) return File.ReadAllText(path);
            }
            catch { }
            return "";
        }

        public void LogCorrection(string whatWrong, string correctAnswer)
        {
            try
            {
                string path = Path.Combine(BasePath, "corrections.md");
                string entry = $"| {DateTime.Now:yyyy-MM-dd} | {whatWrong.Replace("|", " ")} | {correctAnswer.Replace("|", " ")} | Pending |\n";
                File.AppendAllText(path, entry);
                UpdateIndex();
            }
            catch { }
        }

        public void UpdateMemory(string section, string content)
        {
            try
            {
                string path = Path.Combine(BasePath, "memory.md");
                List<string> lines = File.Exists(path) ? File.ReadAllLines(path).ToList() : new List<string>();

                // 针对称呼/身份进行的冲突检测逻辑
                bool isIdentity = content.Contains("叫我") || content.Contains("称呼") || content.Contains("名字") || content.Contains("身份");

                if (isIdentity && section == "Preferences")
                {
                    // 移除旧的同类称呼记忆
                    lines.RemoveAll(l => (l.Contains("叫我") || l.Contains("称呼") || l.Contains("名字") || l.Contains("身份")) && l.StartsWith("-"));
                }

                if (!lines.Any(l => l.Contains(content)))
                {
                    // 如果文件为空或没有该部分，简单处理（这里为了保持简单直接操作字符串）
                    string fullText = string.Join("\n", lines);
                    if (!fullText.Contains($"## {section}"))
                    {
                        fullText += $"\n## {section}\n- {content} ({DateTime.Now:yyyy-MM-dd})\n";
                    }
                    else
                    {
                        // 在对应标题下插入
                        string sectionHeader = $"## {section}";
                        int index = fullText.IndexOf(sectionHeader);
                        if (index != -1)
                        {
                            int endOfHeader = fullText.IndexOf('\n', index);
                            if (endOfHeader == -1) endOfHeader = fullText.Length;
                            fullText = fullText.Insert(endOfHeader, $"\n- {content} ({DateTime.Now:yyyy-MM-dd})");
                        }
                    }
                    File.WriteAllText(path, fullText);
                }
                UpdateIndex();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SelfImproving] UpdateMemory Error: {ex.Message}");
            }
        }

        private void UpdateIndex()
        {
             // refresh index.md
        }

        public string GetSoulSteering()
        {
            string steering = @"【智能行为准则 (v1.2.16)】:
1. 持续进化：从纠错中学习是你的职责。
2. 尊重记忆：必须严格遵守 HOT 记忆中的用户偏好。
3. 身份一致：如果记忆记载了用户是你的XXX，说话时必须维持此关系。
4. 纠错记录：当用户指出错误时，立即意识到并承诺改进。";

            // 如果基础指令太长，仅在后台反思时使用全量，在此处使用缩略版
            return steering;
        }
    }
}
