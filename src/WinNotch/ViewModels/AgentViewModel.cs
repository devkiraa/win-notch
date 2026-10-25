using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinNotch.Models;
using WinNotch.Services;

namespace WinNotch.ViewModels
{
    public partial class AgentViewModel : ObservableObject
    {
        private readonly CodingAgentService _agentService;

        public ObservableCollection<AgentInfo> ActiveAgents { get; } = new();
        public ObservableCollection<InstalledAgent> InstalledAgents { get; } = new();
        public ObservableCollection<AgentTaskHistory> CompletedTasks { get; } = new();

        [ObservableProperty]
        private int _activeCount;

        [ObservableProperty]
        private int _installedCount;

        [ObservableProperty]
        private int _completedCount;

        [ObservableProperty]
        private string _activeHeadline = "All agents idle";

        [ObservableProperty]
        private string _currentAction = "Ready for instructions";

        [ObservableProperty]
        private bool _isAnyAgentRunning;

        [ObservableProperty]
        private string _pillDisplay = "All Idle";

        [ObservableProperty]
        private AgentInfo? _primaryRunningAgent;

        public AgentViewModel(CodingAgentService agentService)
        {
            _agentService = agentService;
            _agentService.AgentsUpdated += OnAgentsUpdated;
        }

        private void OnAgentsUpdated(List<AgentInfo> active, List<InstalledAgent> installed, List<AgentTaskHistory> completed, string headline)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                // 1. Active running agents
                ActiveAgents.Clear();
                foreach (var a in active)
                {
                    a.UpdateElapsed();
                    ActiveAgents.Add(a);
                }

                // 2. Installed agents catalog
                InstalledAgents.Clear();
                foreach (var i in installed)
                {
                    InstalledAgents.Add(i);
                }

                // 3. Completed history
                CompletedTasks.Clear();
                foreach (var c in completed)
                {
                    CompletedTasks.Add(c);
                }

                ActiveCount = active.Count;
                InstalledCount = installed.Count(i => i.IsInstalled);
                CompletedCount = completed.Count;
                IsAnyAgentRunning = active.Count > 0;
                ActiveHeadline = headline;
                PrimaryRunningAgent = active.FirstOrDefault();

                int workingCount = active.Count(a => a.IsRunning);
                if (workingCount > 0)
                {
                    var working = active.First(a => a.IsRunning);
                    CurrentAction = working.CurrentAction;
                    PillDisplay = active.Count > 1
                        ? $"{working.AgentName}: {working.CurrentAction} (+{active.Count - 1} idle)"
                        : $"{working.AgentName}: {working.CurrentAction}";
                }
                else if (active.Count == 1)
                {
                    CurrentAction = "Ready for instructions";
                    PillDisplay = $"{active[0].AgentName}: Ready for instructions";
                }
                else if (active.Count > 1)
                {
                    CurrentAction = $"{active.Count} chats ready";
                    PillDisplay = $"{active.Count} Chats Ready";
                }
                else
                {
                    CurrentAction = "Ready for instructions";
                    PillDisplay = "All Idle";
                }
            });
        }

        [RelayCommand]
        public void KillAgent(AgentInfo? agent)
        {
            if (agent == null) return;

            // If it's a CLI process (proc-<pid>), kill the process tree
            if (agent.Id.StartsWith("proc-") && agent.ProcessId > 0)
            {
                _agentService.KillProcess(agent.ProcessId);
            }
            else
            {
                // Dismiss from active list
                _agentService.DismissChat(agent.Id);
                ActiveAgents.Remove(agent);
                ActiveCount = ActiveAgents.Count;
                IsAnyAgentRunning = ActiveAgents.Count > 0;
            }
        }

        [RelayCommand]
        public void ClearHistory()
        {
            CompletedTasks.Clear();
            CompletedCount = 0;
        }
    }
}
