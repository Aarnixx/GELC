# Godot 4 Live Collaboration MVP

A real-time collaboration system that allows exactly two users to edit the same Godot project simultaneously.

## Features

- **Real-time property syncing**: Inspector changes and transforms sync live
- **Node creation/deletion**: Add or remove nodes and see changes instantly
- **Presence awareness**: See what the other user is selecting
- **Last-write-wins**: Simple conflict resolution with 50ms batching
- **Lightweight**: Only sends changes, never full scene state

## Setup Instructions

### 1. Install the Plugin

1. Copy all `.cs` files and `plugin.cfg` to your Godot project:
   ```
   addons/live_collab/
   ├── LiveCollabPlugin.cs
   ├── NetworkClient.cs
   ├── ChangeBuffer.cs
   ├── ChangeApplier.cs
   ├── PresenceOverlay.cs
   └── plugin.cfg
   ```

2. In Godot, go to **Project → Project Settings → Plugins**

3. Enable the "Live Collaboration" plugin

### 2. Set Up the Relay Server

#### Option A: Local Testing (Node.js)

1. Install Node.js if you haven't already

2. Install WebSocket library:
   ```bash
   npm install ws
   ```

3. Run the relay server:
   ```bash
   node relay-server.js
   ```

#### Option B: Deploy to Hetzner VPS

1. SSH into your VPS:
   ```bash
   ssh user@your-vps-ip
   ```

2. Install Node.js and npm:
   ```bash
   curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
   sudo apt-get install -y nodejs
   ```

3. Copy `relay-server.js` to your VPS

4. Install dependencies:
   ```bash
   npm install ws
   ```

5. Run with PM2 (for production):
   ```bash
   sudo npm install -g pm2
   pm2 start relay-server.js --name "godot-relay"
   pm2 save
   pm2 startup
   ```

6. Open port 8080:
   ```bash
   sudo ufw allow 8080
   ```

### 3. Configure the Plugin

1. Open `LiveCollabPlugin.cs`

2. Set your user ID:
   - User A: `private string _userId = "A";`
   - User B: `private string _userId = "B";`

3. Set the server URL:
   - Local: `private const string SERVER_URL = "ws://localhost:8080";`
   - Remote: `private const string SERVER_URL = "ws://your-vps-ip:8080";`

4. Rebuild the C# project in Godot

## Usage

1. **Start the relay server** (if not already running)

2. **Open Godot on both machines**
   - Make sure both have the same project
   - Both should have the plugin enabled
   - One configured as user "A", the other as user "B"

3. **Open the same scene** on both editors

4. **Start editing!**
   - Select nodes to show your presence
   - Edit properties in the inspector
   - Transform objects in the viewport
   - Add or remove nodes

All changes will sync automatically with 50ms batching.

## How It Works

### Architecture

```
Godot Editor A                    Godot Editor B
      |                                 |
      | WebSocket                       | WebSocket
      |                                 |
      +----------> Relay Server <-------+
                  (Forwards only)
```

### Change Flow

1. User edits a property
2. Change is queued locally
3. Every 50ms, pending changes are batched
4. Batch is sent to relay server via WebSocket
5. Relay forwards to other client
6. Other client applies changes immediately

### Conflict Resolution

- **Last-write-wins**: The last batch received determines final state
- **No validation**: All changes are applied immediately
- **No history**: No undo/redo syncing

## Limitations

- Only 2 users supported
- No asset or file syncing
- No shared undo history
- Requires same project version on both machines
- No intelligent conflict merging
- Transform gizmos may not update perfectly

## Troubleshooting

### Connection Issues

1. Check relay server is running:
   ```bash
   # Should see "Relay server is ready"
   ```

2. Check firewall allows port 8080

3. Verify SERVER_URL in plugin code

### Changes Not Syncing

1. Check console output for errors
2. Ensure both users have same scene open
3. Verify user IDs are different ("A" and "B")
4. Check WebSocket connection status in console

### Performance Issues

- Increase batch interval in `ChangeBuffer.cs` (default: 50ms)
- Reduce number of simultaneous property changes
- Close other resource-intensive programs

## Development Notes

### File Structure

- `LiveCollabPlugin.cs`: Main plugin entry point, hooks editor events
- `NetworkClient.cs`: WebSocket communication with relay server
- `ChangeBuffer.cs`: Batches changes every 50ms
- `ChangeApplier.cs`: Applies incoming changes to scene
- `PresenceOverlay.cs`: Visual feedback for remote user selection

### Extending the System

To add new change types:

1. Define the change structure in `NetworkClient.cs`
2. Add serialization in `NetworkClient.cs`
3. Add application logic in `ChangeApplier.cs`
4. Hook the editor event in `LiveCollabPlugin.cs`

### Future Improvements

- Support for 3+ users
- Operational transformation for better conflict resolution
- Asset syncing
- Shared undo/redo
- Better presence visualization
- Scene tree diff algorithm
- Authentication and permissions

## License

Proprietary Software - All rights reserved. See main LICENSE file for details.
