namespace WinNotch.Models
{
    public enum NotchState
    {
        Idle,           // Sleek small notch/pill (160x32)
        AgentPill,      // Active coding agent indicator (280x32)
        MusicPill,      // Compact media info with mini waveform (260x32)
        AlertPill,      // Micro-notification pill (Caps Lock / Charging: 200x32)
        AgentsExpanded, // Live AI coding agents monitor (380x280)
        MusicExpanded,  // Large media player (380x172)
        TodoExpanded,   // Todo checklist (380x280)
        FocusExpanded,  // Focus timer (380x210)
        ClipsExpanded,  // Clipboard history (380x280)
        Settings        // Settings view (380x280)
    }
}
