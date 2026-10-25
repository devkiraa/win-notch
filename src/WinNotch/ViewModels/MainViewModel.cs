using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinNotch.Models;
using WinNotch.Services;

namespace WinNotch.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public AgentViewModel Agent { get; }
        public MediaViewModel Media { get; }
        public TodoViewModel Todo { get; }
        public TimerViewModel Focus { get; }
        public ClipboardViewModel Clips { get; }
        public SystemStatusService SystemStatus { get; }
        public CodingAgentService CodingAgent { get; }

        [ObservableProperty]
        private NotchState _currentState = NotchState.Idle;

        [ObservableProperty]
        private string _activeTab = "Agents";

        [ObservableProperty]
        private bool _isExpanded;

        [ObservableProperty]
        private double _notchWidth = 160;

        [ObservableProperty]
        private double _notchHeight = 32;

        [ObservableProperty]
        private double _cornerRadiusValue = 16;

        [ObservableProperty]
        private bool _runOnStartup;

        [ObservableProperty]
        private bool _autoHideOnFullscreen = true;

        [ObservableProperty]
        private bool _hasActiveAlert;

        [ObservableProperty]
        private string _alertMessage = string.Empty;

        [ObservableProperty]
        private string _alertSymbol = "🤖";

        private int _alertToken = 0;

        public MainViewModel(
            AgentViewModel agent,
            MediaViewModel media,
            TodoViewModel todo,
            TimerViewModel focus,
            ClipboardViewModel clips,
            SystemStatusService systemStatus,
            CodingAgentService codingAgent)
        {
            Agent = agent;
            Media = media;
            Todo = todo;
            Focus = focus;
            Clips = clips;
            SystemStatus = systemStatus;
            CodingAgent = codingAgent;

            RunOnStartup = StartupService.IsRunOnStartupEnabled();
            AutoHideOnFullscreen = SystemStatus.AutoHideOnFullscreen;

            // React to agent changes when collapsed
            Agent.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(AgentViewModel.IsAnyAgentRunning) ||
                    e.PropertyName == nameof(AgentViewModel.CurrentAction))
                {
                    if (!IsExpanded)
                    {
                        UpdateCollapsedState();
                    }
                }
            };

            // React to coding agent completed task
            CodingAgent.TaskFinished += (agentName, taskSummary, isSuccess) =>
            {
                string sym = isSuccess ? "✅" : "❌";
                TriggerAlert($"{agentName}: {taskSummary}", sym);
            };

            // React to media changes when collapsed
            Media.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MediaViewModel.HasMedia) || e.PropertyName == nameof(MediaViewModel.IsPlaying))
                {
                    if (!IsExpanded)
                    {
                        UpdateCollapsedState();
                    }
                }
            };

            // React to focus timer changes when collapsed
            Focus.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TimerViewModel.IsRunning) || e.PropertyName == nameof(TimerViewModel.PillDisplay))
                {
                    if (!IsExpanded)
                    {
                        UpdateCollapsedState();
                    }
                }
            };

            // Micro-alerts (Caps Lock, Battery plug/unplug)
            SystemStatus.AlertTriggered += (msg, symbol) =>
            {
                TriggerAlert(msg, symbol);
            };

            UpdateDimensions();
        }

        public void TriggerAlert(string message, string symbol)
        {
            Application.Current?.Dispatcher.Invoke(async () =>
            {
                int currentToken = ++_alertToken;
                AlertMessage = message;
                AlertSymbol = symbol;
                HasActiveAlert = true;

                if (!IsExpanded)
                {
                    CurrentState = NotchState.AlertPill;
                    UpdateDimensions();
                }

                await Task.Delay(2600);

                if (currentToken == _alertToken)
                {
                    HasActiveAlert = false;
                    if (!IsExpanded)
                    {
                        UpdateCollapsedState();
                    }
                }
            });
        }

        partial void OnRunOnStartupChanged(bool value)
        {
            StartupService.SetRunOnStartup(value);
        }

        partial void OnAutoHideOnFullscreenChanged(bool value)
        {
            SystemStatus.AutoHideOnFullscreen = value;
        }

        partial void OnIsExpandedChanged(bool value)
        {
            if (value)
            {
                CurrentState = ActiveTab switch
                {
                    "Agents" => NotchState.AgentsExpanded,
                    "Todo" => NotchState.TodoExpanded,
                    "Focus" => NotchState.FocusExpanded,
                    "Clips" => NotchState.ClipsExpanded,
                    "Settings" => NotchState.Settings,
                    _ => NotchState.MusicExpanded
                };
            }
            else
            {
                UpdateCollapsedState();
            }
            UpdateDimensions();
        }

        partial void OnActiveTabChanged(string value)
        {
            if (IsExpanded)
            {
                CurrentState = value switch
                {
                    "Agents" => NotchState.AgentsExpanded,
                    "Todo" => NotchState.TodoExpanded,
                    "Focus" => NotchState.FocusExpanded,
                    "Clips" => NotchState.ClipsExpanded,
                    "Settings" => NotchState.Settings,
                    _ => NotchState.MusicExpanded
                };
                UpdateDimensions();
            }
        }

        private void UpdateCollapsedState()
        {
            if (IsExpanded) return;

            if (HasActiveAlert)
            {
                CurrentState = NotchState.AlertPill;
            }
            else if (Agent.IsAnyAgentRunning)
            {
                CurrentState = NotchState.AgentPill;
            }
            else if (Media.HasMedia || Focus.IsRunning)
            {
                CurrentState = NotchState.MusicPill;
            }
            else
            {
                CurrentState = NotchState.Idle;
            }
            UpdateDimensions();
        }

        private void UpdateDimensions()
        {
            switch (CurrentState)
            {
                case NotchState.Idle:
                    NotchWidth = 160;
                    NotchHeight = 32;
                    CornerRadiusValue = 16;
                    break;
                case NotchState.AlertPill:
                    NotchWidth = 240;
                    NotchHeight = 32;
                    CornerRadiusValue = 16;
                    break;
                case NotchState.AgentPill:
                    NotchWidth = Media.HasMedia ? 300 : 280;
                    NotchHeight = 32;
                    CornerRadiusValue = 16;
                    break;
                case NotchState.MusicPill:
                    NotchWidth = Agent.IsAnyAgentRunning ? 300 : (Focus.IsRunning && Media.HasMedia ? 300 : (Focus.IsRunning ? 220 : 260));
                    NotchHeight = 32;
                    CornerRadiusValue = 16;
                    break;
                case NotchState.AgentsExpanded:
                    NotchWidth = 380;
                    NotchHeight = 280;
                    CornerRadiusValue = 32;
                    break;
                case NotchState.MusicExpanded:
                    NotchWidth = 380;
                    NotchHeight = 172;
                    CornerRadiusValue = 32;
                    break;
                case NotchState.TodoExpanded:
                    NotchWidth = 380;
                    NotchHeight = 280;
                    CornerRadiusValue = 32;
                    break;
                case NotchState.FocusExpanded:
                    NotchWidth = 380;
                    NotchHeight = 230;
                    CornerRadiusValue = 32;
                    break;
                case NotchState.ClipsExpanded:
                    NotchWidth = 380;
                    NotchHeight = 280;
                    CornerRadiusValue = 32;
                    break;
                case NotchState.Settings:
                    NotchWidth = 380;
                    NotchHeight = 280;
                    CornerRadiusValue = 32;
                    break;
            }
        }

        public event Action? RequestResetPosition;

        [RelayCommand]
        private void ResetPosition()
        {
            RequestResetPosition?.Invoke();
        }

        [RelayCommand]
        private void ToggleExpand()
        {
            IsExpanded = !IsExpanded;
        }

        [RelayCommand]
        private void Collapse()
        {
            IsExpanded = false;
        }

        [RelayCommand]
        private void SetTab(string tab)
        {
            ActiveTab = tab;
            IsExpanded = true;
        }

        [RelayCommand]
        private void ExitApp()
        {
            Application.Current?.Shutdown();
        }
    }
}
