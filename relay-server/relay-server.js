const WebSocket = require('ws');

const PORT = 8080;
const MAX_CLIENTS = 2;

const wss = new WebSocket.Server({ port: PORT });

let clients = [];

console.log(`LiveCollab Relay Server listening on port ${PORT}`);
console.log(`Waiting for ${MAX_CLIENTS} clients to connect...`);

wss.on('connection', (ws) => {
    // Check if we already have the maximum number of clients
    if (clients.length >= MAX_CLIENTS) {
        console.log('Connection rejected: Maximum clients reached');
        ws.close(1008, 'Maximum clients reached');
        return;
    }

    // Add client
    clients.push(ws);
    const clientId = clients.length;
    console.log(`Client ${clientId} connected (${clients.length}/${MAX_CLIENTS})`);

    // Handle incoming messages
    ws.on('message', (data) => {
        const message = data.toString();
        
        // Forward to all other clients
        clients.forEach((client) => {
            if (client !== ws && client.readyState === WebSocket.OPEN) {
                client.send(message);
            }
        });
    });

    // Handle client disconnect
    ws.on('close', () => {
        const index = clients.indexOf(ws);
        if (index > -1) {
            clients.splice(index, 1);
            console.log(`Client disconnected (${clients.length}/${MAX_CLIENTS})`);
        }
    });

    // Handle errors
    ws.on('error', (error) => {
        console.error('WebSocket error:', error);
    });
});

// Handle server errors
wss.on('error', (error) => {
    console.error('Server error:', error);
});

console.log('Relay server is ready');
