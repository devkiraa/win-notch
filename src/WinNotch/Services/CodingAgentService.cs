using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using WinNotch.Models;

namespace WinNotch.Services
{
    public class CodingAgentService
    {
        private readonly DispatcherTimer _pollTimer;
        private readonly List<AgentTaskHistory> _history = new();
        private readonly List<string> _brainPaths = new();
        private readonly Dictionary<string, int> _lastTranscriptSteps = new();
        private readonly Dictionary<string, string> _chatTitles = new();

        public event Action<List<AgentInfo>, List<InstalledAgent>, List<AgentTaskHistory>, string>? AgentsUpdated;
        public event Action<string, string, bool>? TaskFinished; // agentName, taskSummary, isSuccess

        public CodingAgentService()
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _brainPaths.Add(Path.Combine(userProfile, ".gemini", "antigravity-ide", "brain"));
            _brainPaths.Add(Path.Combine(userProfile, ".gemini", "antigravity", "brain"));
            _brainPaths.Add(Path.Combine(userProfile, ".gemini", "antigravity-cli", "brain"));

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
                var allProcs = Process.GetProcesses();
                bool isAntigravityRunning = allProcs.Any(p => p.ProcessName.Contains("Antigravity", StringComparison.OrdinalIgnoreCase));

                // 1. Scan running CLI / terminal processes
                ScanAgentProcesses(activeAgents, allProcs);

                // 2. Scan Antigravity IDE brain activity
                ScanAntigravityBrain(activeAgents, isAntigravityRunning);

                // 3. Scan installed coding agents catalog
                var installedAgents = ScanInstalledAgents(activeAgents, isAntigravityRunning);

                // Compute headline
                string headline = "All agents idle";
                var working = activeAgents.FirstOrDefault(a => a.IsRunning);
                if (working != null)
                {
                    headline = $"{working.AgentName}: {working.CurrentAction}";
                }
                else if (activeAgents.Count > 0)
                {
                    headline = activeAgents.Count == 1
                        ? $"{activeAgents[0].AgentName}: Ready for instructions"
                        : $"{activeAgents.Count} chats ready";
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

        private List<InstalledAgent> ScanInstalledAgents(List<AgentInfo> activeAgents, bool isAntigravityRunning)
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
                if (item.Id == "antigravity")
                {
                    item.IsRunning = isAntigravityRunning || activeAgents.Any(a => a.LogoKey == "Antigravity");
                }
                else
                {
                    item.IsRunning = activeAgents.Any(a => a.AgentName.Contains(item.Name, StringComparison.OrdinalIgnoreCase) ||
                                                           item.Name.Contains(a.AgentName, StringComparison.OrdinalIgnoreCase) ||
                                                           string.Equals(a.LogoKey, item.LogoKey, StringComparison.OrdinalIgnoreCase));
                }
            }

            return catalog;
        }

        private void ScanAgentProcesses(List<AgentInfo> activeAgents, Process[] allProcs)
        {
            try
            {
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

        private void ScanAntigravityBrain(List<AgentInfo> activeAgents, bool isAntigravityRunning)
        {
            try
            {
                var now = DateTime.Now;
                var allConversations = new List<(DirectoryInfo Dir, string TranscriptPath, FileInfo FileInfo)>();

                foreach (var brainPath in _brainPaths)
                {
                    if (!Directory.Exists(brainPath)) continue;

                    var brainDir = new DirectoryInfo(brainPath);
                    var convDirs = brainDir.GetDirectories()
                        .Where(d => d.Name != "tempmediaStorage")
                        .ToList();

                    foreach (var conv in convDirs)
                    {
                        var transcriptPath = Path.Combine(conv.FullName, ".system_generated", "logs", "transcript.jsonl");
                        if (File.Exists(transcriptPath))
                        {
                            allConversations.Add((conv, transcriptPath, new FileInfo(transcriptPath)));
                        }
                    }
                }

                // Sort newest to oldest
                allConversations.Sort((a, b) => b.FileInfo.LastWriteTime.CompareTo(a.FileInfo.LastWriteTime));

                var addedChatIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 1. Scan local conversations from brain paths
                foreach (var item in allConversations)
                {
                    try
                    {
                        double ageSec = (now - item.FileInfo.LastWriteTime).TotalSeconds;
                        // Extended active window: 120s ensures ongoing agent turn is not prematurely marked idle
                        bool isChatWorking = ageSec >= 0 && ageSec < 120;
                        double ageHours = (now - item.FileInfo.LastWriteTime).TotalHours;

                        var (stepIndex, type, status, toolSummary, hasRealTool) = ReadLatestTranscriptLine(item.TranscriptPath);
                        string chatTitle = GetChatTitle(item.Dir.Name, item.TranscriptPath);

                        bool shouldInclude = isChatWorking || (isAntigravityRunning && ageHours < 24 && addedChatIds.Count < 3);

                        if (shouldInclude && addedChatIds.Add(item.Dir.Name))
                        {
                            string actionText = isChatWorking
                                ? (string.IsNullOrWhiteSpace(toolSummary) ? "Thinking..." : toolSummary)
                                : "Ready for instructions";

                            activeAgents.Add(new AgentInfo
                            {
                                Id = $"antigravity-{item.Dir.Name}",
                                ProcessId = Process.GetCurrentProcess().Id,
                                AgentName = chatTitle,
                                LogoKey = "Antigravity",
                                AccentColor = isChatWorking ? "#10B981" : "#8B5CF6",
                                BgColor = "#140E28",
                                CurrentAction = actionText,
                                StartedAt = item.FileInfo.LastWriteTime,
                                IsRunning = isChatWorking,
                                StatusColor = isChatWorking ? "#10B981" : "#71717A"
                            });
                        }

                        // Track completed steps
                        int lastStep = _lastTranscriptSteps.TryGetValue(item.Dir.Name, out int s) ? s : -1;
                        if (stepIndex > lastStep)
                        {
                            bool isThinking = !hasRealTool ||
                                              string.IsNullOrWhiteSpace(toolSummary) ||
                                              toolSummary.Equals("Thinking...", StringComparison.OrdinalIgnoreCase) ||
                                              toolSummary.Equals("Executing tool", StringComparison.OrdinalIgnoreCase) ||
                                              toolSummary.StartsWith("Thinking", StringComparison.OrdinalIgnoreCase);

                            if (!isThinking && lastStep != -1)
                            {
                                var lastHistory = _history.FirstOrDefault();
                                if (lastHistory == null || !string.Equals(lastHistory.ActionName, toolSummary, StringComparison.OrdinalIgnoreCase))
                                {
                                    var historyItem = new AgentTaskHistory
                                    {
                                        AgentName = chatTitle,
                                        ActionName = toolSummary,
                                        CompletedAt = DateTime.Now,
                                        IsSuccess = status != "ERROR",
                                        DurationText = "Done"
                                    };
                                    _history.Insert(0, historyItem);
                                    TaskFinished?.Invoke(chatTitle, toolSummary, status != "ERROR");
                                }
                            }
                            _lastTranscriptSteps[item.Dir.Name] = stepIndex;
                        }
                    }
                    catch
                    {
                        // Skip locked conversation
                    }
                }

                // 2. Scan VS Code / Antigravity workspaceStorage (handles remote SSH/WSL workspaces like HistPrune-ViT)
                if (isAntigravityRunning)
                {
                    ScanWorkspaceStorage(activeAgents, addedChatIds);
                }

                // Fallback if Antigravity is open but no active/recent conversation found
                if (activeAgents.Count == 0 && isAntigravityRunning)
                {
                    var latest = allConversations.FirstOrDefault();
                    string fallbackTitle = latest.Dir != null
                        ? GetChatTitle(latest.Dir.Name, latest.TranscriptPath)
                        : "Antigravity IDE";

                    activeAgents.Add(new AgentInfo
                    {
                        Id = "antigravity-idle",
                        ProcessId = Process.GetCurrentProcess().Id,
                        AgentName = fallbackTitle,
                        LogoKey = "Antigravity",
                        AccentColor = "#8B5CF6",
                        BgColor = "#140E28",
                        CurrentAction = "Ready for instructions",
                        StartedAt = DateTime.Now,
                        IsRunning = false,
                        StatusColor = "#71717A"
                    });
                }
            }
            catch { }
        }

        private void ScanWorkspaceStorage(List<AgentInfo> activeAgents, HashSet<string> addedChatIds)
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var wsStorageDir = Path.Combine(appData, "Antigravity IDE", "User", "workspaceStorage");
                if (!Directory.Exists(wsStorageDir)) return;

                var now = DateTime.Now;
                var dirInfo = new DirectoryInfo(wsStorageDir);
                var workspaces = dirInfo.GetDirectories()
                    .OrderByDescending(d => d.LastWriteTime)
                    .Take(15);

                foreach (var ws in workspaces)
                {
                    try
                    {
                        var dbPath = Path.Combine(ws.FullName, "state.vscdb");
                        if (!File.Exists(dbPath)) continue;

                        var dbFi = new FileInfo(dbPath);
                        double ageSec = (now - dbFi.LastWriteTime).TotalSeconds;
                        double ageHours = (now - dbFi.LastWriteTime).TotalHours;

                        // Only consider workspaces modified recently (last 8 hours)
                        if (ageHours > 8) continue;

                        // Check if this workspace has a custom session title or project folder
                        string title = ExtractWorkspaceTitle(ws.FullName);
                        if (string.IsNullOrWhiteSpace(title)) continue;

                        // Check if already represented in activeAgents
                        if (activeAgents.Any(a => a.AgentName.Equals(title, StringComparison.OrdinalIgnoreCase) ||
                                                 a.AgentName.Contains(title, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }

                        if (addedChatIds.Add(ws.Name))
                        {
                            // If state.vscdb was touched in last 120s, workspace session is actively working!
                            bool isWorking = ageSec >= 0 && ageSec < 120;

                            activeAgents.Add(new AgentInfo
                            {
                                Id = $"ws-{ws.Name}",
                                ProcessId = Process.GetCurrentProcess().Id,
                                AgentName = title,
                                LogoKey = "Antigravity",
                                AccentColor = isWorking ? "#10B981" : "#8B5CF6",
                                BgColor = "#140E28",
                                CurrentAction = isWorking ? "Running tasks..." : "Ready for instructions",
                                StartedAt = dbFi.LastWriteTime,
                                IsRunning = isWorking,
                                StatusColor = isWorking ? "#10B981" : "#71717A"
                            });
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private string ExtractWorkspaceTitle(string wsDirectory)
        {
            string title = "";
            try
            {
                var dbPath = Path.Combine(wsDirectory, "state.vscdb");
                if (File.Exists(dbPath))
                {
                    using var fs = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(fs, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                    string content = reader.ReadToEnd();

                    // Find description in artifactsEditorInput or history.entries
                    var matches = Regex.Matches(content, @"antigravity\.artifactsEditorInput.*?description"":""([^""]{3,80})""", RegexOptions.Singleline);
                    foreach (Match m in matches)
                    {
                        var val = m.Groups[1].Value.Trim();
                        if (IsValidChatTitle(val))
                        {
                            return CleanTitle(val);
                        }
                    }

                    // Fallback to any valid description in state.vscdb
                    var descMatches = Regex.Matches(content, @"""description"":""([^""]{3,80})""");
                    foreach (Match m in descMatches)
                    {
                        var val = m.Groups[1].Value.Trim();
                        if (IsValidChatTitle(val))
                        {
                            return CleanTitle(val);
                        }
                    }
                }

                // If no session description found, use the clean folder name from workspace.json
                var wsJson = Path.Combine(wsDirectory, "workspace.json");
                if (File.Exists(wsJson))
                {
                    var jsonContent = File.ReadAllText(wsJson);
                    using var doc = JsonDocument.Parse(jsonContent);
                    if (doc.RootElement.TryGetProperty("folder", out var folderProp))
                    {
                        var folder = folderProp.GetString() ?? "";
                        var folderName = folder.TrimEnd('/').Split('/').Last();
                        folderName = Uri.UnescapeDataString(folderName);
                        if (!string.IsNullOrWhiteSpace(folderName))
                        {
                            return CleanTitle(folderName);
                        }
                    }
                }
            }
            catch { }

            return title;
        }

        private static bool IsValidChatTitle(string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return false;
            if (val.Equals("Artifact", StringComparison.OrdinalIgnoreCase)) return false;
            if (val.StartsWith("You are", StringComparison.OrdinalIgnoreCase)) return false;
            if (val.StartsWith("I will", StringComparison.OrdinalIgnoreCase)) return false;
            if (val.StartsWith("Enables ", StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        private static string CleanTitle(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Antigravity";
            var clean = raw.Trim();
            if (clean.Length > 35) clean = clean.Substring(0, 32).TrimEnd() + "...";
            return clean;
        }

        private static (int StepIndex, string Type, string Status, string ToolSummary, bool HasRealTool) ReadLatestTranscriptLine(string transcriptPath)
        {
            try
            {
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

                if (lines.Count == 0) return (0, "", "", "Thinking...", false);

                var latestLine = lines[^1];
                using var doc = JsonDocument.Parse(latestLine);
                var root = doc.RootElement;

                int stepIndex = root.TryGetProperty("step_index", out var s) ? s.GetInt32() : 0;
                string type = root.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                string status = root.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "";

                string toolSummary = "Thinking...";
                bool hasRealTool = false;

                if (root.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.GetArrayLength() > 0)
                {
                    var firstTool = toolCalls[0];
                    if (firstTool.TryGetProperty("args", out var args))
                    {
                        if (args.TryGetProperty("toolAction", out var act))
                        {
                            var actStr = act.GetString()?.Trim('\"');
                            if (!string.IsNullOrWhiteSpace(actStr))
                            {
                                toolSummary = actStr;
                                hasRealTool = true;
                            }
                        }
                        else if (args.TryGetProperty("toolSummary", out var summ))
                        {
                            var summStr = summ.GetString()?.Trim('\"');
                            if (!string.IsNullOrWhiteSpace(summStr))
                            {
                                toolSummary = summStr;
                                hasRealTool = true;
                            }
                        }
                    }
                }

                return (stepIndex, type, status, toolSummary, hasRealTool);
            }
            catch
            {
                return (0, "", "", "Thinking...", false);
            }
        }

        private string GetChatTitle(string convId, string transcriptPath)
        {
            if (_chatTitles.TryGetValue(convId, out var cached))
            {
                return cached;
            }

            string title = "";
            try
            {
                using var fs = new FileStream(transcriptPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs);

                string? line0 = reader.ReadLine();
                if (!string.IsNullOrWhiteSpace(line0))
                {
                    // 1. Try matching workspace/repository path in transcript context
                    var matchRepo = Regex.Match(line0, @"(?:Repository|workspace|projects?)[\\/]([a-zA-Z0-9_\-\.]+)", RegexOptions.IgnoreCase);
                    if (matchRepo.Success && matchRepo.Groups.Count > 1)
                    {
                        var repo = matchRepo.Groups[1].Value.Trim();
                        if (!string.Equals(repo, "Repository", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(repo, "workspace", StringComparison.OrdinalIgnoreCase))
                        {
                            title = CleanTitle(repo);
                        }
                    }

                    if (string.IsNullOrWhiteSpace(title))
                    {
                        var matchCorpus = Regex.Match(line0, @"->\s*([a-zA-Z0-9_\-\./]+)");
                        if (matchCorpus.Success && matchCorpus.Groups.Count > 1)
                        {
                            var name = matchCorpus.Groups[1].Value.Trim().Split('/').Last();
                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                title = CleanTitle(name);
                            }
                        }
                    }

                    // 2. Extract clean request summary from user prompt in step 0
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(line0);
                            if (doc.RootElement.TryGetProperty("content", out var contentProp))
                            {
                                var content = contentProp.GetString() ?? "";
                                var promptMatch = Regex.Match(content, @"<USER_REQUEST>(.*?)</USER_REQUEST>", RegexOptions.Singleline);
                                var reqText = promptMatch.Success ? promptMatch.Groups[1].Value : content;

                                // Strip slash commands (/plan, /goal, etc.)
                                reqText = Regex.Replace(reqText, @"^/(?:plan|goal|commit|review|test|fix|audit)\s+", "", RegexOptions.IgnoreCase).Trim();
                                reqText = Regex.Replace(reqText, @"<[^>]+>", "").Trim();

                                var firstSentence = reqText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                                firstSentence = firstSentence.Trim();

                                // Strip filler beginnings
                                string[] fillers = new[] { "i want to build a ", "i want to build ", "build a ", "build ", "help me build a ", "help me build ", "act as " };
                                foreach (var f in fillers)
                                {
                                    if (firstSentence.StartsWith(f, StringComparison.OrdinalIgnoreCase))
                                    {
                                        firstSentence = firstSentence.Substring(f.Length).Trim();
                                        break;
                                    }
                                }

                                if (!string.IsNullOrWhiteSpace(firstSentence))
                                {
                                    if (char.IsLower(firstSentence[0]))
                                    {
                                        firstSentence = char.ToUpper(firstSentence[0]) + firstSentence.Substring(1);
                                    }
                                    title = CleanTitle(firstSentence);
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            if (string.IsNullOrWhiteSpace(title))
            {
                title = "Antigravity Chat";
            }

            _chatTitles[convId] = title;
            return title;
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
