using Godot;
using System;

namespace LiveCollab
{
    public partial class PresenceOverlay : GodotObject
    {
        private string _remoteSelectedPath = "";
        private string _remoteTool = "";

        public void UpdatePresence(PresenceMessage presence)
        {
            _remoteSelectedPath = presence.Selected;
            _remoteTool = presence.Tool;

            // For MVP, we just log the presence
            // In a full implementation, this would draw visual overlays in the editor
            if (!string.IsNullOrEmpty(_remoteSelectedPath))
            {
                GD.Print($"LiveCollab: Remote user selected {_remoteSelectedPath} with tool {_remoteTool}");
            }
        }

        public string GetRemoteSelectedPath()
        {
            return _remoteSelectedPath;
        }

        public string GetRemoteTool()
        {
            return _remoteTool;
        }

        public void Clear()
        {
            _remoteSelectedPath = "";
            _remoteTool = "";
        }
    }
}
