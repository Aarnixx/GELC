// LiveCollab Configuration Template
// Copy the relevant sections into your LiveCollabPlugin.cs

// ============================================
// USER CONFIGURATION
// ============================================

// Set to "A" for first user, "B" for second user
// IMPORTANT: Each user must have a different ID
private string _userId = "A";  // Change to "B" on the other machine

// ============================================
// SERVER CONFIGURATION
// ============================================

// For local testing (same machine or local network)
private const string SERVER_URL = "ws://localhost:8080";

// For internet collaboration (replace with your server IP/domain)
// private const string SERVER_URL = "ws://your-server-ip:8080";
// private const string SERVER_URL = "ws://your-domain.com:8080";

// For secure WebSocket (if you've set up SSL/TLS)
// private const string SERVER_URL = "wss://your-domain.com:8080";

// ============================================
// ADVANCED CONFIGURATION
// ============================================

// Change these values in the respective files:

// ChangeBuffer.cs - Batch interval
// private const double BATCH_INTERVAL = 0.05; // 50ms (default)
// Lower = more responsive but more network traffic
// Higher = less network traffic but more latency
// Recommended range: 0.03 (30ms) to 0.1 (100ms)

// NetworkClient.cs - Connection timeout (if implementing)
// private const int CONNECTION_TIMEOUT = 5000; // 5 seconds

// ============================================
// RELAY SERVER CONFIGURATION
// ============================================

// In relay-server.js:

// Port number
// const PORT = 8080; // Default
// const PORT = 3000; // Alternative

// Maximum clients
// const MAX_CLIENTS = 2; // MVP only supports 2
// Do not change unless modifying the protocol

// ============================================
// NETWORK OPTIMIZATION
// ============================================

// For high-latency connections (e.g., intercontinental):
// - Increase BATCH_INTERVAL to 0.1 (100ms)
// - This reduces network packets and improves stability

// For low-latency connections (e.g., LAN):
// - Decrease BATCH_INTERVAL to 0.03 (30ms)
// - This improves responsiveness

// For limited bandwidth:
// - Increase BATCH_INTERVAL to 0.15 (150ms)
// - Consider reducing change frequency in your workflow

// ============================================
// DEBUGGING OPTIONS
// ============================================

// To enable verbose logging, add these to NetworkClient.cs:

// In Process() method, after receiving a message:
// GD.Print($"Received message: {json.Substring(0, Math.Min(100, json.Length))}...");

// In SendBatch() method, before sending:
// GD.Print($"Sending batch with {batch.Changes.Count} changes");

// To disable presence logging, in PresenceOverlay.cs:
// Comment out the GD.Print line in UpdatePresence()

// ============================================
// PRODUCTION CHECKLIST
// ============================================

/*
Before using in production:

[ ] Change SERVER_URL to your production server
[ ] Enable WSS (secure WebSocket)
[ ] Implement authentication
[ ] Add rate limiting
[ ] Add message validation
[ ] Set up server monitoring
[ ] Configure automatic server restart (PM2)
[ ] Set up SSL certificates (Let's Encrypt)
[ ] Test with multiple network conditions
[ ] Document rollback procedure
*/

// ============================================
// EXAMPLE CONFIGURATIONS
// ============================================

// Example 1: Local Testing on Same Machine
// User A:
//   private string _userId = "A";
//   private const string SERVER_URL = "ws://localhost:8080";
// User B:
//   private string _userId = "B";
//   private const string SERVER_URL = "ws://localhost:8080";

// Example 2: LAN Collaboration
// Both users:
//   private string _userId = "A"; // or "B"
//   private const string SERVER_URL = "ws://192.168.1.100:8080";
//   (Replace 192.168.1.100 with the server's local IP)

// Example 3: Internet Collaboration
// Both users:
//   private string _userId = "A"; // or "B"
//   private const string SERVER_URL = "ws://collab.example.com:8080";

// Example 4: Production with SSL
// Both users:
//   private string _userId = "A"; // or "B"
//   private const string SERVER_URL = "wss://collab.example.com:8080";
