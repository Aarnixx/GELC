# System Architecture Diagram

## Component Overview

```mermaid
graph TB
    subgraph "Godot Editor A"
        A1[LiveCollabPlugin]
        A2[ChangeBuffer]
        A3[NetworkClient]
        A4[ChangeApplier]
        A5[PresenceOverlay]
        
        A1 --> A2
        A1 --> A3
        A1 --> A4
        A1 --> A5
        A2 --> A3
    end
    
    subgraph "Godot Editor B"
        B1[LiveCollabPlugin]
        B2[ChangeBuffer]
        B3[NetworkClient]
        B4[ChangeApplier]
        B5[PresenceOverlay]
        
        B1 --> B2
        B1 --> B3
        B1 --> B4
        B1 --> B5
        B2 --> B3
    end
    
    subgraph "Relay Server"
        R[WebSocket Server<br/>Port 8080]
    end
    
    A3 <-->|WebSocket| R
    B3 <-->|WebSocket| R
    
    style A1 fill:#4CAF50
    style B1 fill:#4CAF50
    style R fill:#2196F3
```

## Message Flow - Outgoing Change

```mermaid
sequenceDiagram
    participant User as User A
    participant Editor as Godot Editor
    participant Plugin as LiveCollabPlugin
    participant Buffer as ChangeBuffer
    participant Network as NetworkClient
    participant Server as Relay Server
    participant EditorB as Godot Editor B
    
    User->>Editor: Edits property
    Editor->>Plugin: OnPropertyEdited()
    Plugin->>Buffer: QueueChange()
    
    Note over Buffer: Wait 50ms
    
    Plugin->>Buffer: ProcessBuffer()
    Buffer->>Network: SendBatch()
    Network->>Server: WebSocket send
    Server->>EditorB: Forward message
    
    Note over EditorB: Apply changes
```

## Message Flow - Incoming Change

```mermaid
sequenceDiagram
    participant Server as Relay Server
    participant Network as NetworkClient
    participant Plugin as LiveCollabPlugin
    participant Applier as ChangeApplier
    participant Scene as Godot Scene
    
    Server->>Network: WebSocket message
    Network->>Network: Parse JSON
    Network->>Plugin: OnBatchReceived()
    Plugin->>Plugin: Set isApplyingRemote = true
    Plugin->>Applier: ApplyBatch()
    
    loop For each change
        Applier->>Scene: Apply change
    end
    
    Plugin->>Plugin: Set isApplyingRemote = false
```

## State Diagram

```mermaid
stateDiagram-v2
    [*] --> Disconnected
    Disconnected --> Connecting: Connect()
    Connecting --> Connected: Server accepts
    Connecting --> Disconnected: Connection failed
    
    Connected --> Editing: Scene open
    Editing --> Sending: Change made
    Sending --> Editing: Batch sent
    
    Editing --> Receiving: Message received
    Receiving --> Editing: Changes applied
    
    Connected --> Disconnected: Close connection
    Disconnected --> [*]
```

## Data Flow

```mermaid
flowchart LR
    A[User Edit] --> B[Editor Signal]
    B --> C{isApplyingRemote?}
    C -->|No| D[Queue Change]
    C -->|Yes| E[Ignore]
    D --> F[ChangeBuffer]
    F -->|50ms| G[Create Batch]
    G --> H[Serialize JSON]
    H --> I[WebSocket Send]
    I --> J[Relay Server]
    J --> K[Other Client]
    K --> L[Deserialize]
    L --> M[Apply Changes]
    M --> N[Update Scene]
```

## Network Topology Options

### Option 1: Local Testing
```
┌─────────────┐         ┌─────────────┐
│  Editor A   │         │  Editor B   │
│ localhost   │         │ localhost   │
└──────┬──────┘         └──────┬──────┘
       │                       │
       └───────┬───────────────┘
               │
        ┌──────▼──────┐
        │   Server    │
        │  localhost  │
        │   :8080     │
        └─────────────┘
```

### Option 2: LAN Setup
```
┌─────────────┐         ┌─────────────┐
│  Editor A   │         │  Editor B   │
│ 192.168.1.2 │         │ 192.168.1.3 │
└──────┬──────┘         └──────┬──────┘
       │                       │
       └───────┬───────────────┘
               │
        ┌──────▼──────┐
        │   Server    │
        │192.168.1.10 │
        │   :8080     │
        └─────────────┘
```

### Option 3: Internet Setup
```
┌─────────────┐         ┌─────────────┐
│  Editor A   │         │  Editor B   │
│   USA       │         │   Europe    │
└──────┬──────┘         └──────┬──────┘
       │                       │
       │   Internet            │
       └───────┬───────────────┘
               │
        ┌──────▼──────┐
        │   Server    │
        │ VPS/Cloud   │
        │   :8080     │
        └─────────────┘
```

## Change Types

```mermaid
classDiagram
    class Change {
        <<abstract>>
        +type: string
    }
    
    class PropertyChange {
        +type: "set_property"
        +node: string
        +property: string
        +value: any
    }
    
    class AddNodeChange {
        +type: "add_node"
        +parent: string
        +name: string
        +scene: string
    }
    
    class RemoveNodeChange {
        +type: "remove_node"
        +node: string
    }
    
    class PresenceMessage {
        +type: "presence"
        +selected: string
        +tool: string
    }
    
    Change <|-- PropertyChange
    Change <|-- AddNodeChange
    Change <|-- RemoveNodeChange
    Change <|-- PresenceMessage
```

## Conflict Resolution Timeline

```mermaid
gantt
    title Last-Write-Wins Example
    dateFormat X
    axisFormat %L ms
    
    section User A
    Edit property     :0, 10
    Queue change      :10, 20
    Wait for batch    :20, 50
    Send batch        :50, 70
    
    section User B
    Edit same property:20, 30
    Queue change      :30, 40
    Wait for batch    :40, 70
    Send batch        :70, 90
    
    section Network
    Receive A's batch :70, 80
    Receive B's batch :90, 100
    
    section Result
    Final value is B's:100, 100
```

## Performance Metrics

```mermaid
pie title Network Bandwidth Distribution
    "JSON Overhead" : 30
    "Property Values" : 50
    "Metadata" : 15
    "Presence" : 5
```

## Error Handling Flow

```mermaid
flowchart TD
    A[Receive Message] --> B{Valid JSON?}
    B -->|No| C[Log Error]
    B -->|Yes| D{Node Exists?}
    D -->|No| E[Log Warning & Skip]
    D -->|Yes| F{Valid Property?}
    F -->|No| G[Log Error & Skip]
    F -->|Yes| H[Apply Change]
    H --> I{Success?}
    I -->|No| J[Log Error]
    I -->|Yes| K[Continue]
    
    C --> L[End]
    E --> L
    G --> L
    J --> L
    K --> L
```
