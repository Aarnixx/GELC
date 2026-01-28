# Quick Start Guide - 5 Minutes to Collaborative Editing

## Prerequisites
- Godot 4.x with C# support
- Node.js installed
- Two machines (or two Godot instances on the same machine)

## Step 1: Start the Relay Server (2 minutes)

```bash
# Navigate to the relay server directory
cd path/to/relay-server

# Install dependencies (first time only)
npm install

# Start the server
npm start
```

You should see:
```
LiveCollab Relay Server listening on port 8080
Waiting for 2 clients to connect...
Relay server is ready
```

## Step 2: Install the Plugin (1 minute)

**On BOTH machines:**

1. Create the plugin directory in your Godot project:
   ```
   your_project/
   └── addons/
       └── live_collab/
   ```

2. Copy these files into `addons/live_collab/`:
   - LiveCollabPlugin.cs
   - NetworkClient.cs
   - ChangeBuffer.cs
   - ChangeApplier.cs
   - PresenceOverlay.cs
   - plugin.cfg

## Step 3: Configure the Plugin (1 minute)

**Machine A:**
```csharp
// In LiveCollabPlugin.cs
private string _userId = "A";
private const string SERVER_URL = "ws://localhost:8080"; // or your server IP
```

**Machine B:**
```csharp
// In LiveCollabPlugin.cs
private string _userId = "B";
private const string SERVER_URL = "ws://localhost:8080"; // or your server IP
```

## Step 4: Enable the Plugin (1 minute)

**On BOTH machines:**

1. Open Godot
2. Go to **Project → Project Settings → Plugins**
3. Find "Live Collaboration" and enable it
4. Click **Build** to compile the C# scripts (Project → Tools → C# → Build Solution)

## Step 5: Start Collaborating!

**On BOTH machines:**

1. Open the same scene (e.g., the main scene of your project)
2. Check the console - you should see:
   ```
   LiveCollab: Initializing plugin
   LiveCollab: Connecting to ws://...
   LiveCollab: Connected to relay server
   ```

**Now try:**
- Select a node → See it logged on the other machine
- Move an object → See it move on the other machine
- Change a property → See it update on the other machine
- Add a node → See it appear on the other machine

## Troubleshooting

### "Connection Failed"
- Is the relay server running? Check the terminal
- Is the SERVER_URL correct?
- Firewall blocking port 8080?

### "Changes Not Syncing"
- Are both machines editing the SAME scene?
- Are user IDs different? (One "A", one "B")
- Check console for errors

### "Plugin Not Appearing"
- Did you copy all 6 files?
- Is plugin.cfg in the correct location?
- Try restarting Godot

## Testing Your Setup

1. **On Machine A**: Select a node in the scene tree
2. **On Machine B**: Check the console - you should see:
   ```
   LiveCollab: Remote user selected /root/YourNode with tool select
   ```

3. **On Machine A**: Move a 3D object in the viewport
4. **On Machine B**: Watch it move in real-time!

## What's Next?

- Read `README.md` for detailed usage
- Read `TECHNICAL_DOCS.md` to understand how it works
- Deploy the relay server to a VPS for internet collaboration
- Customize the batching interval in `ChangeBuffer.cs`

## Pro Tips

**Lower latency**: Deploy relay server geographically close to both users

**Better visualization**: The presence overlay currently just logs - extend `PresenceOverlay.cs` to draw visual indicators

**Debugging**: Watch the console output on both machines to see what's syncing

**Production**: Add authentication and validation before using with sensitive projects

---

You're now set up for real-time collaboration!

