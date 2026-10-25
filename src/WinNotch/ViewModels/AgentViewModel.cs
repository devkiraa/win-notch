using System.Collections.ObjectModel;
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
        public ObservableCollection<AgentTaskHistory> CompletedTasks { get; } = new();

        [ObservableProperty]
        private int _activeCount;

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

        public AgentViewModel(CodingAgentService agentService)
        {
            _agentService = agentService;
            _agentService.AgentsUpdated += OnAgentsUpdated;
        }

        private void OnAgentsUpdated(System.Collections.Generic.List<AgentInfo> active, System.Collections.Generic.List<AgentTaskHistory> completed, string headline)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                ActiveAgents.Clear();
                foreach (var a in active)
                {
                    a.UpdateElapsed();
                    ActiveAgents.Add(a);
                }

                CompletedTasks.Clear();
                foreach (var c in completed)
                {
                    CompletedTasks.Add(c);
                }

                ActiveCount = active.Count;
                CompletedCount = completed.Count;
                IsAnyAgentRunning = active.Count > 0;
                ActiveHeadline = headline;

                if (active.Count > 0)
                {
                    CurrentAction = active[0].CurrentAction;
                    PillDisplay = $"{active[0].AgentName}: {active[0].CurrentAction}";
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
            if (agent == null || agent.ProcessId <= 0) return;
            _agentService.KillProcess(agent.ProcessId);
        }

        [RelayCommand]
        public void ClearHistory()
        {
            CompletedTasks.Clear();
            CompletedCount = 0;
        }
    }
}
