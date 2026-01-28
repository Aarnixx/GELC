#if TOOLS
using Godot;
using System;

namespace LiveCollab
{
    [Tool]
    public partial class LiveCollabPlugin : EditorPlugin
    {
        private NetworkClient _networkClient;
        private ChangeBuffer _changeBuffer;
        private ChangeApplier _changeApplier;
        private PresenceOverlay _presenceOverlay;
        
        private string _userId = "A"; // Set to "A" or "B" - should be configurable
        private const string SERVER_URL = "ws://192.168.0.245:8080"; // Change to your relay server
        
        private bool _isApplyingRemote = false;
        private Node _selectedNode = null;

        public override void _EnterTree()
        {
            GD.Print("LiveCollab: Initializing plugin");
            
            // Initialize components
            _changeBuffer = new ChangeBuffer();
            _changeApplier = new ChangeApplier(GetEditorInterface());
            _presenceOverlay = new PresenceOverlay();
            
            // Initialize network client with callbacks
            _networkClient = new NetworkClient(SERVER_URL, _userId);
            _networkClient.OnBatchReceived += HandleBatchReceived;
            _networkClient.OnPresenceReceived += HandlePresenceReceived;
            _networkClient.OnConnected += () => GD.Print("LiveCollab: Connected to relay server");
            _networkClient.OnDisconnected += () => GD.Print("LiveCollab: Disconnected from relay server");
            
            // Connect to relay server
            _networkClient.Connect();
            
            // Hook editor signals
            var editorSelection = GetEditorInterface().GetSelection();
            editorSelection.SelectionChanged += OnSelectionChanged;
            
            // Note: In Godot 4.x, PropertyEdited signal works differently
            // We'll need to use EditorInspector's property_edited signal
            var inspector = GetEditorInterface().GetInspector();
            if (inspector != null)
            {
                inspector.Connect("property_edited", Callable.From<string>(OnPropertyEdited));
            }
            
            GD.Print("LiveCollab: Plugin initialized");
        }

        public override void _ExitTree()
        {
            GD.Print("LiveCollab: Shutting down plugin");
            
            // Disconnect signals
            var editorSelection = GetEditorInterface().GetSelection();
            editorSelection.SelectionChanged -= OnSelectionChanged;
            
            var inspector = GetEditorInterface().GetInspector();
            if (inspector != null && inspector.IsConnected("property_edited", Callable.From<string>(OnPropertyEdited)))
            {
                inspector.Disconnect("property_edited", Callable.From<string>(OnPropertyEdited));
            }
            
            // Cleanup network
            _networkClient?.Disconnect();
            
            GD.Print("LiveCollab: Plugin shut down");
        }

        public override void _Process(double delta)
        {
            // Update network client (process WebSocket messages)
            _networkClient?.Process();
            
            // Process change buffer (batch and send changes every 50ms)
            if (_changeBuffer != null && !_isApplyingRemote)
            {
                var batch = _changeBuffer.ProcessBuffer(delta);
                if (batch != null)
                {
                    _networkClient?.SendBatch(batch);
                }
            }
        }

        private void OnSelectionChanged()
        {
            if (_isApplyingRemote)
                return;
                
            var selection = GetEditorInterface().GetSelection();
            var selectedNodes = selection.GetSelectedNodes();
            
            if (selectedNodes.Count > 0)
            {
                _selectedNode = selectedNodes[0] as Node;
                if (_selectedNode != null)
                {
                    string nodePath = _selectedNode.GetPath();
                    
                    // Send presence message immediately (not batched)
                    _networkClient?.SendPresence(nodePath, "select");
                }
            }
            else
            {
                _selectedNode = null;
                _networkClient?.SendPresence("", "select");
            }
        }

        private void OnPropertyEdited(string property)
        {
            if (_isApplyingRemote || _selectedNode == null)
                return;
                
            string nodePath = _selectedNode.GetPath();
            var value = _selectedNode.Get(property);
            
            // Queue the property change
            var change = new PropertyChange
            {
                Type = "set_property",
                Node = nodePath,
                Property = property,
                Value = value
            };
            
            _changeBuffer.QueueChange(change);
        }

        private void HandleBatchReceived(ChangeBatch batch)
        {
            // Apply all changes in the batch
            _isApplyingRemote = true;
            
            try
            {
                _changeApplier.ApplyBatch(batch);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"LiveCollab: Error applying batch: {ex.Message}");
            }
            finally
            {
                _isApplyingRemote = false;
            }
        }

        private void HandlePresenceReceived(PresenceMessage presence)
        {
            // Update visual overlay
            _presenceOverlay.UpdatePresence(presence);
        }
    }
}
#endif
