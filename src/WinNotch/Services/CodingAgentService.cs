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
        private string _lastActiveConversation = string.Empty;
        private int _lastTranscriptStep = -1;
        private DateTime _lastTaskCheckTime = DateTime.Now;

        public event Action<List<AgentInfo>, List<AgentTaskHistory>, string>? AgentsUpdated;
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

                // 1. Scan running process list for terminal coding agents (agy, claude, aider, etc.)
                ScanAgentProcesses(activeAgents);

                // 2. Scan Antigravity IDE brain for active operations
                ScanAntigravityBrain(activeAgents);

                // Compute headline
                string headline = "All agents idle";
                if (activeAgents.Count > 0)
                {
                    var primary = activeAgents[0];
                    headline = $"{primary.AgentName}: {primary.CurrentAction}";
                }

                AgentsUpdated?.Invoke(activeAgents, _history.Take(25).ToList(), headline);
            }
            catch
            {
                // Ignore transient polling read locks
            }
        }

        private void ScanAgentProcesses(List<AgentInfo> activeAgents)
        {
            try
            {
                var targets = new[] { "agy", "claude", "aider", "cursor", "gemini" };
                var allProcs = Process.GetProcesses();

                foreach (var proc in allProcs)
                {
                    try
                    {
                        var name = proc.ProcessName.ToLowerInvariant();
                        bool isMatch = false;
                        string agentLabel = "Agent";

                        if (name.Contains("agy"))
                        {
                            isMatch = true;
                            agentLabel = "agy (Terminal)";
                        }
                        else if (name.Contains("claude"))
                        {
                            isMatch = true;
                            agentLabel = "Claude Code";
                        }
                        else if (name.Contains("aider"))
                        {
                            isMatch = true;
                            agentLabel = "Aider";
                        }

                        if (isMatch)
                        {
                            var started = proc.StartTime;
                            activeAgents.Add(new AgentInfo
                            {
                                Id = $"proc-{proc.Id}",
                                ProcessId = proc.Id,
                                AgentName = agentLabel,
                                CurrentAction = string.IsNullOrWhiteSpace(proc.MainWindowTitle)
                                    ? "Running in terminal"
                                    : proc.MainWindowTitle,
                                StartedAt = started,
                                IsRunning = true,
                                StatusColor = "#3478F6"
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
                // Find most recently modified conversation directory
                var brainDir = new DirectoryInfo(_brainPath);
                var latestConv = brainDir.GetDirectories()
                    .Where(d => d.Name != "tempmediaStorage")
                    .OrderByDescending(d => d.LastWriteTime)
                    .FirstOrDefault();

                if (latestConv == null) return;

                var logsPath = Path.Combine(latestConv.FullName, ".system_generated", "logs");
                var transcriptPath = Path.Combine(logsPath, "transcript.jsonl");

                if (!File.Exists(transcriptPath)) return;

                // Read last few lines with FileShare.ReadWrite to prevent locking
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

                // Check latest step
                var latestLine = lines[^1];
                using var doc = JsonDocument.Parse(latestLine);
                var root = doc.RootElement;

                int stepIndex = root.TryGetProperty("step_index", out var s) ? s.GetInt32() : 0;
                string type = root.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                string status = root.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "";

                // Check for tool calls in the latest step
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
                        CurrentAction = currentToolSummary,
                        StartedAt = latestConv.LastWriteTime,
                        IsRunning = true,
                        StatusColor = "#8B5CF6"
                    });
                }

                // Check if step changed to record completion history
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
