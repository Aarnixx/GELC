# Godot 4 Live Collaboration MVP

Real-time collaborative editing for Godot 4 - allowing exactly two users to edit the same project simultaneously.

![Version](https://img.shields.io/badge/version-1.0.0-blue)
![Godot](https://img.shields.io/badge/Godot-4.x-blue)
![License](https://img.shields.io/badge/license-MIT-green)

##  Features

- **Real-time synchronization** - See changes as they happen
- **Property syncing** - Inspector and transform changes sync live
- **Node operations** - Add/remove nodes collaboratively
- **Presence awareness** - See what your collaborator is selecting
- **Last-write-wins** - Simple, predictable conflict resolution
- **Lightweight** - Only sends changes, not full scene state
- **50ms batching** - Optimized network traffic

##  Project Structure

```
godot-live-collab/
├── addons/
│   └── live_collab/          # Godot plugin (copy to your project)
│       ├── LiveCollabPlugin.cs
│       ├── NetworkClient.cs
│       ├── ChangeBuffer.cs
│       ├── ChangeApplier.cs
│       ├── PresenceOverlay.cs
│       └── plugin.cfg
├── relay-server/             # WebSocket relay server
│   ├── relay-server.js
│   ├── package.json
│   └── test-relay.js
├── docs/                     # Documentation
│   ├── README.md            # Detailed usage guide
│   ├── QUICKSTART.md        # 5-minute setup guide
│   ├── TECHNICAL_DOCS.md    # Technical documentation
│   └── CONFIG_TEMPLATE.cs   # Configuration examples
├── LICENSE
└── .gitignore
```

##  Quick Start

### 1. Start the Relay Server

```bash
cd relay-server
npm install
npm start
```

### 2. Install the Plugin

Copy `addons/live_collab/` to your Godot project's `addons/` directory.

### 3. Configure Users

**Machine A** - In `LiveCollabPlugin.cs`:
```csharp
private string _userId = "A";
private const string SERVER_URL = "ws://localhost:8080";
```

**Machine B** - In `LiveCollabPlugin.cs`:
```csharp
private string _userId = "B";
private const string SERVER_URL = "ws://localhost:8080";
```

### 4. Enable Plugin

In Godot: **Project → Project Settings → Plugins** → Enable "Live Collaboration"

### 5. Start Editing!

Open the same scene on both machines and start collaborating!

##  Documentation

- **[Quick Start Guide](docs/QUICKSTART.md)** - Get started in 5 minutes
- **[Full Documentation](docs/README.md)** - Detailed usage and deployment
- **[Technical Documentation](docs/TECHNICAL_DOCS.md)** - Architecture and internals
- **[Configuration Template](docs/CONFIG_TEMPLATE.cs)** - Configuration examples

##  What Works

 Property changes (position, rotation, scale, materials, etc.)  
 Node creation and deletion  
 Real-time transform gizmo updates  
 Inspector property editing  
 Presence indicators (console logging)  
 50ms change batching for performance  

##  Limitations (MVP)

- Only 2 users supported
- No asset/file syncing
- No shared undo history
- No authentication (add before production)
- Basic presence (console only, no visual overlays yet)

##  Requirements

**Plugin**:
- Godot 4.x with C# support
- .NET SDK

**Relay Server**:
- Node.js 16+
- npm

##  Network Topology

```
Godot Editor A ←→ Relay Server ←→ Godot Editor B
```

The relay server:
- Accepts exactly 2 WebSocket connections
- Forwards all messages between clients
- Stores no state
- Performs no validation

##  Testing

Test the relay server:
```bash
cd relay-server
node test-relay.js
```

This runs automated tests to verify the server is working correctly.

## Security Notice

WARNING: This MVP has NO security features:
- No authentication
- No authorization
- No message validation
- No rate limiting

**Do not use in production without adding security!**

See [Technical Documentation](docs/TECHNICAL_DOCS.md) for security recommendations.

##  Example Use Cases

- **Collaborative level design** - Build levels together in real-time
- **Remote pair programming** - Work on scenes with a teammate
- **Teaching/mentoring** - Show students how to use Godot
- **Rapid prototyping** - Quickly iterate with a designer

##  Deployment Options

### Local Network (LAN)
Perfect for: Office collaboration, game jams, workshops
```bash
SERVER_URL = "ws://192.168.1.100:8080"
```

### Internet (VPS)
Perfect for: Remote teams, distributed collaboration
```bash
SERVER_URL = "ws://your-vps-ip:8080"
```

See [Full Documentation](docs/README.md) for VPS deployment instructions.

##  Troubleshooting

**Connection failed?**
- Is the relay server running?
- Check firewall settings (port 8080)
- Verify SERVER_URL is correct

**Changes not syncing?**
- Are both editors on the same scene?
- Are user IDs different? (A vs B)
- Check console for error messages

**Performance issues?**
- Increase batch interval in `ChangeBuffer.cs`
- Reduce simultaneous edits
- Check network latency

## Future Enhancements

- Support for 3+ users
- Visual presence overlays (3D gizmos, highlights)
- Asset and file syncing
- Operational transformation for better conflict resolution
- Shared undo/redo history
- Authentication and authorization
- Voice chat integration
- Scene diff algorithm

## License

Proprietary Software - See [LICENSE](LICENSE) file for details

All rights reserved. Unauthorized copying, distribution, or modification is prohibited.

## Support

Having issues? Check:
1. [Quick Start Guide](docs/QUICKSTART.md)
2. [Troubleshooting section](docs/README.md#troubleshooting)
3. [Technical Documentation](docs/TECHNICAL_DOCS.md)

---

Made with Godot 4 | Built for collaboration | Powered by WebSockets

