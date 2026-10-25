using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Threading;
using WinNotch.Models;

namespace WinNotch.Services
{
    public class CodingAgentService
    {
        private readonly DispatcherTimer _pollTimer;
        private readonly List<AgentTaskHistory> _history = new();
        private readonly string _brainPath;
        private int _lastTranscriptStep = -1;

        public event Action<List<AgentInfo>, List<InstalledAgent>, List<AgentTaskHistory>, string>? AgentsUpdated;
        public event Action<string, string, bool>? TaskFinished; // agentName, taskSummary, isSuccess

        public CodingAgentService()
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _brainPath = Path.Combine(userProfile, ".gemini", "antigravity-ide", "brain");

            _pollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1200)
            };
            _pollTimer.Tick += OnPollTick;
        }

        public void Start()
        {
            _pollTimer.Start();
            PollNow();
        }

        public void Stop()
        {
            _pollTimer.Stop();
        }

        private void OnPollTick(object? sender, EventArgs e)
        {
            PollNow();
        }

        public void PollNow()
        {
            try
            {
                var activeAgents = new List<AgentInfo>();

                // 1. Scan running processes
                ScanAgentProcesses(activeAgents);

                // 2. Scan Antigravity IDE brain activity
                ScanAntigravityBrain(activeAgents);

                // 3. Scan installed coding agents catalog
                var installedAgents = ScanInstalledAgents(activeAgents);

                // Compute headline
                string headline = "All agents idle";
                if (activeAgents.Count > 0)
                {
                    var primary = activeAgents[0];
                    headline = $"{primary.AgentName}: {primary.CurrentAction}";
                }

                AgentsUpdated?.Invoke(activeAgents, installedAgents, _history.Take(25).ToList(), headline);
            }
            catch
            {
                // Ignore transient polling read locks
            }
        }

        private static bool CheckAgentInstalled(string command, params string[] paths)
        {
            foreach (var p in paths)
            {
                if (!string.IsNullOrEmpty(p) && (File.Exists(p) || Directory.Exists(p)))
                {
                    return true;
                }
            }

            try
            {
                var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
                foreach (var dir in pathEnv.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var cleanDir = dir.Trim();
                    if (!Directory.Exists(cleanDir)) continue;

                    if (File.Exists(Path.Combine(cleanDir, command)) ||
                        File.Exists(Path.Combine(cleanDir, command + ".exe")) ||
                        File.Exists(Path.Combine(cleanDir, command + ".cmd")) ||
                        File.Exists(Path.Combine(cleanDir, command + ".ps1")))
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        private List<InstalledAgent> ScanInstalledAgents(List<AgentInfo> activeAgents)
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            var catalog = new List<InstalledAgent>
            {
                new InstalledAgent
                {
                    Id = "antigravity",
                    Name = "Antigravity",
                    TypeLabel = "IDE Engine",
                    ExecutablePath = Path.Combine(userProfile, ".gemini", "antigravity-ide"),
                    IsInstalled = Directory.Exists(Path.Combine(userProfile, ".gemini", "antigravity-ide")),
                    AccentColor = "#8B5CF6",
                    BgColor = "#140E28",
                    LogoKey = "Antigravity"
                },
                new InstalledAgent
                {
                    Id = "agy",
                    Name = "agy CLI",
                    TypeLabel = "Terminal Agent",
                    ExecutablePath = Path.Combine(localAppData, "agy", "bin", "agy.exe"),
                    IsInstalled = CheckAgentInstalled("agy",
                        Path.Combine(localAppData, "agy", "bin", "agy.exe"),
                        Path.Combine(userProfile, ".gemini", "bin", "agy.exe")),
                    AccentColor = "#38BDF8",
                    BgColor = "#082F49",
                    LogoKey = "Agy"
                },
                new InstalledAgent
                {
                    Id = "claude",
                    Name = "Claude Code",
                    TypeLabel = "CLI Agent",
                    ExecutablePath = Path.Combine(userProfile, ".local", "bin", "claude.exe"),
                    IsInstalled = CheckAgentInstalled("claude",
                        Path.Combine(userProfile, ".local", "bin", "claude.exe")),
                    AccentColor = "#D97706",
                    BgColor = "#291605",
                    LogoKey = "Claude"
                },
                new InstalledAgent
                {
                    Id = "cline",
                    Name = "Cline",
                    TypeLabel = "Autonomous Dev",
                    ExecutablePath = Path.Combine(appData, "npm", "cline.cmd"),
                    IsInstalled = CheckAgentInstalled("cline",
                        Path.Combine(appData, "npm", "cline.cmd"),
                        Path.Combine(appData, "npm", "cline.ps1")),
                    AccentColor = "#10B981",
                    BgColor = "#042F2E",
                    LogoKey = "Cline"
                },
                new InstalledAgent
                {
                    Id = "gemini",
                    Name = "Gemini CLI",
                    TypeLabel = "Google AI CLI",
                    ExecutablePath = Path.Combine(appData, "npm", "gemini.cmd"),
                    IsInstalled = CheckAgentInstalled("gemini",
                        Path.Combine(appData, "npm", "gemini.cmd"),
                        Path.Combine(appData, "npm", "gemini.ps1")),
                    AccentColor = "#4285F4",
                    BgColor = "#0D1E3A",
                    LogoKey = "Gemini"
                },
                new InstalledAgent
                {
                    Id = "codebuff",
                    Name = "Codebuff",
                    TypeLabel = "Dev Assistant",
                    ExecutablePath = Path.Combine(appData, "npm", "codebuff.cmd"),
                    IsInstalled = CheckAgentInstalled("codebuff",
                        Path.Combine(appData, "npm", "codebuff.cmd"),
                        Path.Combine(appData, "npm", "codebuff.ps1")),
                    AccentColor = "#EC4899",
                    BgColor = "#380B22",
                    LogoKey = "Codebuff"
                },
                new InstalledAgent
                {
                    Id = "opencode",
                    Name = "OpenCode",
                    TypeLabel = "Pair Programmer",
                    ExecutablePath = Path.Combine(appData, "npm", "opencode.cmd"),
                    IsInstalled = CheckAgentInstalled("opencode",
                        Path.Combine(appData, "npm", "opencode.cmd"),
                        Path.Combine(appData, "npm", "opencode.ps1")),
                    AccentColor = "#F59E0B",
                    BgColor = "#301B05",
                    LogoKey = "OpenCode"
                },
                new InstalledAgent
                {
                    Id = "cursor",
                    Name = "Cursor",
                    TypeLabel = "AI Code Editor",
                    ExecutablePath = Path.Combine(localAppData, "Programs", "cursor", "Cursor.exe"),
                    IsInstalled = CheckAgentInstalled("cursor",
                        Path.Combine(localAppData, "Programs", "cursor", "Cursor.exe")),
                    AccentColor = "#6366F1",
                    BgColor = "#141530",
                    LogoKey = "Cursor"
                }
            };

            // Mark running status
            foreach (var item in catalog)
            {
                bool isRunning = activeAgents.Any(a => a.AgentName.Contains(item.Name, StringComparison.OrdinalIgnoreCase) ||
                                                       item.Name.Contains(a.AgentName, StringComparison.OrdinalIgnoreCase) ||
                                                       string.Equals(a.LogoKey, item.LogoKey, StringComparison.OrdinalIgnoreCase));
                item.IsRunning = isRunning;
            }

            return catalog;
        }

        private void ScanAgentProcesses(List<AgentInfo> activeAgents)
        {
            try
            {
                var allProcs = Process.GetProcesses();

                foreach (var proc in allProcs)
                {
                    try
                    {
                        var name = proc.ProcessName.ToLowerInvariant();
                        bool isMatch = false;
                        string agentLabel = "Agent";
                        string logoKey = "Default";
                        string accentColor = "#3478F6";
                        string bgColor = "#141418";

                        if (name.Contains("agy"))
                        {
                            isMatch = true;
                            agentLabel = "agy CLI";
                            logoKey = "Agy";
                            accentColor = "#38BDF8";
                            bgColor = "#082F49";
                        }
                        else if (name.Contains("claude"))
                        {
                            isMatch = true;
                            agentLabel = "Claude Code";
                            logoKey = "Claude";
                            accentColor = "#D97706";
                            bgColor = "#291605";
                        }
                        else if (name.Contains("cline"))
                        {
                            isMatch = true;
                            agentLabel = "Cline";
                            logoKey = "Cline";
                            accentColor = "#10B981";
                            bgColor = "#042F2E";
                        }
                        else if (name.Contains("aider"))
                        {
                            isMatch = true;
                            agentLabel = "Aider";
                            logoKey = "Aider";
                            accentColor = "#22C55E";
                            bgColor = "#052E16";
                        }

                        if (isMatch)
                        {
                            var started = proc.StartTime;
                            activeAgents.Add(new AgentInfo
                            {
                                Id = $"proc-{proc.Id}",
                                ProcessId = proc.Id,
                                AgentName = agentLabel,
                                LogoKey = logoKey,
                                AccentColor = accentColor,
                                BgColor = bgColor,
                                CurrentAction = string.IsNullOrWhiteSpace(proc.MainWindowTitle)
                                    ? "Executing in terminal"
                                    : proc.MainWindowTitle,
                                StartedAt = started,
                                IsRunning = true,
                                StatusColor = accentColor
                            });
                        }
                    }
                    catch
                    {
                        // Skip processes without query permissions
                    }
                }
            }
            catch { }
        }

        private void ScanAntigravityBrain(List<AgentInfo> activeAgents)
        {
            if (!Directory.Exists(_brainPath)) return;

            try
            {
                var brainDir = new DirectoryInfo(_brainPath);
                var latestConv = brainDir.GetDirectories()
                    .Where(d => d.Name != "tempmediaStorage")
                    .OrderByDescending(d => d.LastWriteTime)
                    .FirstOrDefault();

                if (latestConv == null) return;

                var logsPath = Path.Combine(latestConv.FullName, ".system_generated", "logs");
                var transcriptPath = Path.Combine(logsPath, "transcript.jsonl");

                if (!File.Exists(transcriptPath)) return;

                using var fs = new FileStream(transcriptPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs);

                var lines = new List<string>();
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        lines.Add(line);
                        if (lines.Count > 15) lines.RemoveAt(0);
                    }
                }

                if (lines.Count == 0) return;

                var latestLine = lines[^1];
                using var doc = JsonDocument.Parse(latestLine);
                var root = doc.RootElement;

                int stepIndex = root.TryGetProperty("step_index", out var s) ? s.GetInt32() : 0;
                string type = root.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                string status = root.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "";

                string currentToolSummary = "Thinking...";
                if (root.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.GetArrayLength() > 0)
                {
                    var firstTool = toolCalls[0];
                    if (firstTool.TryGetProperty("args", out var args))
                    {
                        if (args.TryGetProperty("toolAction", out var act))
                        {
                            currentToolSummary = act.GetString()?.Trim('\"') ?? "Executing tool";
                        }
                        else if (args.TryGetProperty("toolSummary", out var summ))
                        {
                            currentToolSummary = summ.GetString()?.Trim('\"') ?? "Executing tool";
                        }
                    }
                }

                bool isRecent = (DateTime.Now - latestConv.LastWriteTime).TotalSeconds < 15;

                if (isRecent)
                {
                    activeAgents.Insert(0, new AgentInfo
                    {
                        Id = "antigravity-active",
                        ProcessId = Process.GetCurrentProcess().Id,
                        AgentName = "Antigravity",
                        LogoKey = "Antigravity",
                        AccentColor = "#8B5CF6",
                        BgColor = "#140E28",
                        CurrentAction = currentToolSummary,
                        StartedAt = latestConv.LastWriteTime,
                        IsRunning = true,
                        StatusColor = "#8B5CF6"
                    });
                }

                if (stepIndex > _lastTranscriptStep)
                {
                    if (_lastTranscriptStep != -1 && (type == "RUN_COMMAND" || type == "PLANNER_RESPONSE"))
                    {
                        bool isSuccess = status != "ERROR";
                        var historyItem = new AgentTaskHistory
                        {
                            AgentName = "Antigravity",
                            ActionName = currentToolSummary,
                            CompletedAt = DateTime.Now,
                            IsSuccess = isSuccess,
                            DurationText = "Done"
                        };
                        _history.Insert(0, historyItem);

                        TaskFinished?.Invoke("Antigravity", currentToolSummary, isSuccess);
                    }
                    _lastTranscriptStep = stepIndex;
                }
            }
            catch { }
        }

        public void KillProcess(int processId)
        {
            try
            {
                var proc = Process.GetProcessById(processId);
                proc.Kill(entireProcessTree: true);
                PollNow();
            }
            catch { }
        }
    }
}
