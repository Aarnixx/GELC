using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

public class NetworkClient
{
    private WebSocketPeer _webSocket;
    private string _serverUrl;
    private string _userId;
    private int _batchId = 0;
    private bool _isConnected = false;

    public Action OnConnected;
    public Action OnDisconnected;
    public Action<ChangeBatch> OnBatchReceived;
    public Action<PresenceMessage> OnPresenceReceived;

    public NetworkClient(string serverUrl, string userId)
    {
        _serverUrl = serverUrl;
        _userId = userId;
        _webSocket = new WebSocketPeer();
    }

    public void Connect()
    {
        var error = _webSocket.ConnectToUrl(_serverUrl);
        if (error != Error.Ok)
        {
            GD.PrintErr($"LiveCollab: Failed to connect to {_serverUrl}: {error}");
        }
        else
        {
            GD.Print($"LiveCollab: Connecting to {_serverUrl}...");
        }
    }

    public void Disconnect()
    {
        if (_webSocket != null)
        {
            _webSocket.Close();
            _isConnected = false;
        }
    }

    public void Process()
    {
        if (_webSocket == null)
            return;

        _webSocket.Poll();
        var state = _webSocket.GetReadyState();

        // Handle connection state changes
        if (state == WebSocketPeer.State.Open && !_isConnected)
        {
            _isConnected = true;
            OnConnected?.Invoke();
        }
        else if (state == WebSocketPeer.State.Closed && _isConnected)
        {
            _isConnected = false;
            OnDisconnected?.Invoke();
        }

        // Process incoming messages
        while (_webSocket.GetAvailablePacketCount() > 0)
        {
            var packet = _webSocket.GetPacket();
            var json = System.Text.Encoding.UTF8.GetString(packet);
            
            try
            {
                HandleMessage(json);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"LiveCollab: Error handling message: {ex.Message}");
            }
        }
    }

    public void SendBatch(ChangeBatch batch)
    {
        if (!_isConnected)
            return;

        var envelope = new MessageEnvelope
        {
            User = _userId,
            BatchId = _batchId++,
            Changes = batch.Changes
        };

        var json = JsonSerializer.Serialize(envelope, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        _webSocket.SendText(json);
    }

    public void SendPresence(string selectedNode, string tool)
    {
        if (!_isConnected)
            return;

        var presence = new PresenceMessage
        {
            Type = "presence",
            Selected = selectedNode,
            Tool = tool
        };

        var envelope = new MessageEnvelope
        {
            User = _userId,
            BatchId = -1, // Presence doesn't use batch ID
            Changes = new List<object> { presence }
        };

        var json = JsonSerializer.Serialize(envelope, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        _webSocket.SendText(json);
    }

    private void HandleMessage(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        // Check if this is from another user
        if (root.TryGetProperty("user", out var userProp))
        {
            var user = userProp.GetString();
            if (user == _userId)
                return; // Ignore our own messages
        }

        // Parse changes array
        if (root.TryGetProperty("changes", out var changesArray))
        {
            var changes = new List<object>();
            
            foreach (var changeElement in changesArray.EnumerateArray())
            {
                if (changeElement.TryGetProperty("type", out var typeProp))
                {
                    var type = typeProp.GetString();
                    
                    if (type == "presence")
                    {
                        var presence = JsonSerializer.Deserialize<PresenceMessage>(changeElement.GetRawText(), new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                        OnPresenceReceived?.Invoke(presence);
                    }
                    else
                    {
                        // Add to changes list for batch processing
                        changes.Add(changeElement.GetRawText());
                    }
                }
            }

            // If we have non-presence changes, create a batch
            if (changes.Count > 0)
            {
                var batch = new ChangeBatch { Changes = changes };
                OnBatchReceived?.Invoke(batch);
            }
        }
    }
}

// Data structures
public class MessageEnvelope
{
    [JsonPropertyName("user")]
    public string User { get; set; }
    
    [JsonPropertyName("batch_id")]
    public int BatchId { get; set; }
    
    [JsonPropertyName("changes")]
    public List<object> Changes { get; set; }
}

public class ChangeBatch
{
    public List<object> Changes { get; set; } = new List<object>();
}

public class PropertyChange
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("node")]
    public string Node { get; set; }
    
    [JsonPropertyName("property")]
    public string Property { get; set; }
    
    [JsonPropertyName("value")]
    public object Value { get; set; }
}

public class AddNodeChange
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("parent")]
    public string Parent { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("scene")]
    public string Scene { get; set; }
}

public class RemoveNodeChange
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("node")]
    public string Node { get; set; }
}

public class PresenceMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("selected")]
    public string Selected { get; set; }
    
    [JsonPropertyName("tool")]
    public string Tool { get; set; }
}
