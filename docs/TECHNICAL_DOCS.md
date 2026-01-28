# Technical Documentation - Godot Live Collaboration MVP

## System Overview

This system implements real-time collaboration for Godot 4 using a client-server architecture with WebSocket communication. The design prioritizes simplicity and predictability through last-write-wins conflict resolution.

## Core Components

### 1. LiveCollabPlugin.cs

**Purpose**: Main entry point and coordinator

**Responsibilities**:
- Initialize all subsystems
- Hook Godot editor signals
- Coordinate message flow between components
- Manage remote application guard flag

**Key Methods**:
- `_EnterTree()`: Plugin initialization
- `_ExitTree()`: Cleanup
- `_Process(delta)`: Main loop for network and buffer processing
- `OnSelectionChanged()`: Handles editor selection events
- `OnPropertyEdited(property)`: Handles inspector property changes

**Important State**:
- `_isApplyingRemote`: Prevents echo loops when applying remote changes

### 2. NetworkClient.cs

**Purpose**: WebSocket communication layer

**Responsibilities**:
- Establish and maintain WebSocket connection
- Serialize outgoing messages to JSON
- Deserialize incoming messages
- Route messages to appropriate handlers

**Message Format**:
```json
{
  "user": "A",
  "batch_id": 123,
  "changes": [
    {
      "type": "set_property",
      "node": "/root/Level/Enemy1",
      "property": "position",
      "value": [1.0, 0.0, 3.5]
    }
  ]
}
```

**Key Methods**:
- `Connect()`: Initiate WebSocket connection
- `Process()`: Poll WebSocket and process messages
- `SendBatch(batch)`: Send batched changes
- `SendPresence(node, tool)`: Send immediate presence update

### 3. ChangeBuffer.cs

**Purpose**: Batch changes for efficient network transmission

**Design Rationale**:
- Reduces network overhead
- Groups related changes
- Provides consistent update rate

**Configuration**:
- Batch interval: 50ms (configurable via `BATCH_INTERVAL`)

**Behavior**:
- Changes are queued immediately
- No deduplication or reordering
- Sends batch when timer expires AND changes exist
- Clears buffer after sending

### 4. ChangeApplier.cs

**Purpose**: Apply incoming changes to the editor scene

**Conflict Resolution**: Last-write-wins
- No validation
- No rollback
- No history tracking
- Immediate application

**Supported Change Types**:

#### set_property
Modifies any property on any node:
```json
{
  "type": "set_property",
  "node": "/root/Level/Player",
  "property": "position",
  "value": [10.0, 0.0, 5.0]
}
```

Handles:
- Transform properties (position, rotation, scale)
- Material properties
- Script variables
- Any inspector-editable property

#### add_node
Creates a new node instance:
```json
{
  "type": "add_node",
  "parent": "/root/Level",
  "name": "Enemy1",
  "scene": "res://Enemy.tscn"
}
```

Process:
1. Locate parent node
2. Load PackedScene
3. Instantiate scene
4. Set name
5. Add as child
6. Set owner to edited scene root

#### remove_node
Deletes a node:
```json
{
  "type": "remove_node",
  "node": "/root/Level/Enemy1"
}
```

Uses `QueueFree()` for safe deferred removal.

**Type Conversion**:
- JSON primitives → Godot Variant
- JSON arrays → Vector2/Vector3/Quaternion detection
- JSON objects → Dictionary

### 5. PresenceOverlay.cs

**Purpose**: Display remote user activity

**Current Implementation**: MVP logging only

**Future Enhancement Possibilities**:
- 3D gizmo overlays
- Node tree highlighting
- User cursors in viewport
- Username labels

## Message Flow Diagrams

### Outgoing Change Flow
```
User edits property
    ↓
OnPropertyEdited() called
    ↓
Change queued in ChangeBuffer
    ↓
_Process() called (every frame)
    ↓
Timer >= 50ms?
    ↓ Yes
ChangeBuffer.ProcessBuffer()
    ↓
NetworkClient.SendBatch()
    ↓
JSON serialization
    ↓
WebSocket send
    ↓
Relay Server
```

### Incoming Change Flow
```
Relay Server
    ↓
WebSocket receive
    ↓
NetworkClient.Process()
    ↓
HandleMessage()
    ↓
JSON deserialization
    ↓
Type == "presence"?
    ↓ Yes          ↓ No
PresenceOverlay    OnBatchReceived
    ↓                  ↓
Display info       Set _isApplyingRemote = true
                       ↓
                   ChangeApplier.ApplyBatch()
                       ↓
                   For each change:
                       ↓
                   Apply to scene
                       ↓
                   Set _isApplyingRemote = false
```

## Network Protocol Specification

### Connection Lifecycle

1. **Connection Establishment**
   ```
   Client → Server: WebSocket handshake
   Server → Client: Accept (if < 2 clients) or Reject
   ```

2. **Active Session**
   ```
   Client A → Server: Message
   Server → Client B: Forward message
   (Server never modifies or stores)
   ```

3. **Disconnection**
   ```
   Client → Server: Close
   Server: Remove from clients list
   ```

### Message Types

#### Batch Message (Authoritative)
```json
{
  "user": "A",
  "batch_id": 42,
  "changes": [...]
}
```
- Sent every 50ms (if changes exist)
- Processed in order
- Applied immediately

#### Presence Message (Non-Authoritative)
```json
{
  "user": "A",
  "batch_id": -1,
  "changes": [
    {
      "type": "presence",
      "selected": "/root/Level/Enemy1",
      "tool": "select"
    }
  ]
}
```
- Sent immediately on selection change
- Not batched
- Used for visual feedback only

## Conflict Resolution Strategy

### Last-Write-Wins (LWW)

**Principle**: The most recently received change is considered authoritative.

**Properties**:
-  Simple to implement
-  Predictable behavior
-  No state synchronization required
-  Can lose user intent
-  No conflict detection

**Example Scenario**:
```
Time  User A          User B          Result
---   ---             ---             ---
0ms   Edit position   -               A's change queued
20ms  -               Edit position   B's change queued
50ms  Send batch A    -               -
60ms  -               Send batch B    -
70ms  Receive B       Receive A       Both see B's value
```

Both editors converge to User B's change because it was received last.

**Edge Cases**:
- **Simultaneous edits**: One change overwrites the other (by network timing)
- **Property deletion**: Setting to null/default is just another write
- **Node deletion during edit**: Change is silently ignored if node doesn't exist

## Performance Characteristics

### Network Bandwidth

**Per Property Change**:
- ~100-200 bytes JSON overhead
- Property value size (varies)

**Typical Batch** (50ms of activity):
- 1-10 changes
- 500-2000 bytes total

**Worst Case** (intensive editing):
- ~20 KB/second per user
- ~40 KB/second total relay traffic

### Latency

**Total Latency Budget**:
```
User A edit → User B sees change
  = Batch delay + Network RTT + Apply time
  ≈ 25ms (avg) + 20-100ms + 1ms
  ≈ 50-130ms typical
```

**Components**:
- Batch delay: 0-50ms (average 25ms)
- Network RTT: 20-100ms (depends on geography)
- Application: <1ms (negligible)

## Security Considerations

### Current State (MVP)
 **No security features implemented**

**Vulnerabilities**:
- No authentication
- No authorization
- No message validation
- No rate limiting
- No encryption (unless using wss://)

### Production Requirements

**Must Implement**:
1. **Authentication**: Verify user identity
2. **Authorization**: Project access control
3. **Validation**: Sanitize all incoming changes
4. **Rate Limiting**: Prevent DoS attacks
5. **Encryption**: Use WSS (WebSocket Secure)

**Recommended Architecture**:
```
Client → Auth Server → Relay Server
         (JWT token)   (validates token)
```

## Debugging and Monitoring

### Godot Console Output

**Connection Events**:
```
LiveCollab: Initializing plugin
LiveCollab: Connecting to ws://localhost:8080...
LiveCollab: Connected to relay server
```

**Change Events**:
```
LiveCollab: Remote user selected /root/Level/Enemy1 with tool select
LiveCollab: Added node Enemy2 to /root/Level
LiveCollab: Removed node /root/Level/Enemy1
```

**Error Events**:
```
LiveCollab: Node not found: /root/Level/MissingNode
LiveCollab: Failed to set property position on /root/Level/Enemy1: ...
```

### Relay Server Console Output

```
LiveCollab Relay Server listening on port 8080
Client 1 connected (1/2)
Client 2 connected (2/2)
Client disconnected (1/2)
```

### Common Issues

**Changes not syncing**:
- Check `_isApplyingRemote` guard
- Verify node paths match exactly
- Ensure scene is open on both editors

**Echo loops**:
- Remote application guard not working
- Verify guard is set before applying changes

**Performance degradation**:
- Too many changes per frame
- Consider increasing batch interval
- Profile with Godot's built-in profiler

## Testing Strategy

### Manual Testing Checklist

**Basic Functionality**:
- [ ] Connect two editors
- [ ] Select node (see presence)
- [ ] Edit property (see change)
- [ ] Move/rotate/scale (see transform)
- [ ] Add node (see addition)
- [ ] Delete node (see removal)

**Edge Cases**:
- [ ] Edit same property simultaneously
- [ ] Delete node being edited
- [ ] Disconnect/reconnect
- [ ] Server restart during session
- [ ] Large property values (arrays, strings)

**Performance**:
- [ ] Rapid property changes
- [ ] Complex scene with many nodes
- [ ] Multiple simultaneous edits
- [ ] Monitor network traffic
- [ ] Check CPU usage

### Automated Testing (Future)

**Unit Tests**:
- ChangeBuffer batching logic
- JSON serialization/deserialization
- Path parsing and node lookup
- Type conversion

**Integration Tests**:
- WebSocket connection lifecycle
- Message routing through relay
- End-to-end change application

## Future Enhancements

### Near-Term
1. **Better presence visualization**: 3D gizmos, node highlighting
2. **Reconnection handling**: Auto-reconnect on disconnect
3. **Scene synchronization**: Ensure both users have same scene

### Medium-Term
1. **More than 2 users**: Relay server supports N clients
2. **Authentication**: User login system
3. **Project versioning**: Ensure compatible versions
4. **Asset syncing**: Share resources and files

### Long-Term
1. **Operational Transformation**: Intelligent conflict resolution
2. **Shared undo/redo**: Synchronized history
3. **Voice chat**: Built-in communication
4. **Scene diff algorithm**: Efficient large-scene updates

## Code Style Guidelines

**C# Conventions**:
- PascalCase for public members
- _camelCase for private fields
- Clear, descriptive names
- Single responsibility per class
- Minimal dependencies

**Error Handling**:
- Try-catch around network operations
- Try-catch around node operations
- Log errors with context
- Never crash the editor

**Performance**:
- Avoid allocations in hot paths
- Reuse collections where possible
- Profile before optimizing
- Batch operations when possible

## Deployment Checklist

**Pre-Deployment**:
- [ ] Test with target Godot version
- [ ] Verify on both Windows and Linux
- [ ] Test on local network
- [ ] Test over internet
- [ ] Document server requirements

**Server Setup**:
- [ ] Install Node.js
- [ ] Install dependencies (npm install)
- [ ] Configure firewall (port 8080)
- [ ] Set up process manager (PM2)
- [ ] Configure auto-start on boot

**Client Setup**:
- [ ] Copy plugin files to project
- [ ] Configure user ID (A or B)
- [ ] Configure server URL
- [ ] Enable plugin in Project Settings
- [ ] Rebuild C# project

**Post-Deployment**:
- [ ] Monitor server logs
- [ ] Monitor client logs
- [ ] Verify connection stability
- [ ] Test all change types
- [ ] Document any issues
