// Test script to verify relay server functionality
// Run with: node test-relay.js

const WebSocket = require('ws');

const SERVER_URL = 'ws://localhost:8080';
const TEST_DURATION = 5000; // 5 seconds

console.log('=== Godot LiveCollab Relay Server Test ===\n');

let clientA, clientB;
let messagesReceivedByA = 0;
let messagesReceivedByB = 0;
let testsPassed = 0;
let testsFailed = 0;

// Test 1: Connect two clients
function test1_connectClients() {
    return new Promise((resolve, reject) => {
        console.log('Test 1: Connecting two clients...');
        
        clientA = new WebSocket(SERVER_URL);
        clientB = new WebSocket(SERVER_URL);
        
        let connectedCount = 0;
        
        clientA.on('open', () => {
            console.log('  ✓ Client A connected');
            connectedCount++;
            if (connectedCount === 2) resolve();
        });
        
        clientB.on('open', () => {
            console.log('  ✓ Client B connected');
            connectedCount++;
            if (connectedCount === 2) resolve();
        });
        
        clientA.on('error', (err) => reject(`Client A error: ${err.message}`));
        clientB.on('error', (err) => reject(`Client B error: ${err.message}`));
        
        setTimeout(() => {
            if (connectedCount !== 2) {
                reject('Timeout: Failed to connect both clients');
            }
        }, 3000);
    });
}

// Test 2: Message forwarding A -> B
function test2_forwardingAtoB() {
    return new Promise((resolve, reject) => {
        console.log('\nTest 2: Testing message forwarding A -> B...');
        
        const testMessage = {
            user: 'A',
            batch_id: 1,
            changes: [
                {
                    type: 'set_property',
                    node: '/root/TestNode',
                    property: 'position',
                    value: [1.0, 2.0, 3.0]
                }
            ]
        };
        
        clientB.once('message', (data) => {
            const received = JSON.parse(data.toString());
            
            if (JSON.stringify(received) === JSON.stringify(testMessage)) {
                console.log('  ✓ Client B received message from A correctly');
                messagesReceivedByB++;
                resolve();
            } else {
                reject('Message content mismatch');
            }
        });
        
        clientA.send(JSON.stringify(testMessage));
        
        setTimeout(() => reject('Timeout: Message not received'), 2000);
    });
}

// Test 3: Message forwarding B -> A
function test3_forwardingBtoA() {
    return new Promise((resolve, reject) => {
        console.log('\nTest 3: Testing message forwarding B -> A...');
        
        const testMessage = {
            user: 'B',
            batch_id: 1,
            changes: [
                {
                    type: 'add_node',
                    parent: '/root/Level',
                    name: 'Enemy1',
                    scene: 'res://Enemy.tscn'
                }
            ]
        };
        
        clientA.once('message', (data) => {
            const received = JSON.parse(data.toString());
            
            if (JSON.stringify(received) === JSON.stringify(testMessage)) {
                console.log('  ✓ Client A received message from B correctly');
                messagesReceivedByA++;
                resolve();
            } else {
                reject('Message content mismatch');
            }
        });
        
        clientB.send(JSON.stringify(testMessage));
        
        setTimeout(() => reject('Timeout: Message not received'), 2000);
    });
}

// Test 4: No echo (A doesn't receive own messages)
function test4_noEcho() {
    return new Promise((resolve, reject) => {
        console.log('\nTest 4: Testing no echo (sender doesn\'t receive own message)...');
        
        let receivedOwnMessage = false;
        
        clientA.once('message', () => {
            receivedOwnMessage = true;
        });
        
        const testMessage = {
            user: 'A',
            batch_id: 2,
            changes: [{ type: 'presence', selected: '/root/Node', tool: 'select' }]
        };
        
        clientA.send(JSON.stringify(testMessage));
        
        // Wait a bit to ensure message isn't received
        setTimeout(() => {
            if (!receivedOwnMessage) {
                console.log('  ✓ Client A did not receive its own message (correct)');
                resolve();
            } else {
                reject('Client A received its own message (echo detected)');
            }
        }, 500);
    });
}

// Test 5: Third client rejection
function test5_thirdClientRejection() {
    return new Promise((resolve, reject) => {
        console.log('\nTest 5: Testing third client rejection...');
        
        const clientC = new WebSocket(SERVER_URL);
        
        clientC.on('close', (code, reason) => {
            if (code === 1008) {
                console.log('  ✓ Third client was rejected correctly');
                resolve();
            } else {
                reject(`Wrong close code: ${code}`);
            }
        });
        
        clientC.on('open', () => {
            reject('Third client was accepted (should have been rejected)');
        });
        
        setTimeout(() => reject('Timeout: Third client not rejected'), 2000);
    });
}

// Test 6: Rapid message burst
function test6_rapidMessages() {
    return new Promise((resolve, reject) => {
        console.log('\nTest 6: Testing rapid message burst...');
        
        const MESSAGE_COUNT = 10;
        let receivedCount = 0;
        
        clientB.on('message', () => {
            receivedCount++;
            if (receivedCount === MESSAGE_COUNT) {
                console.log(`  ✓ All ${MESSAGE_COUNT} rapid messages received`);
                resolve();
            }
        });
        
        // Send burst of messages
        for (let i = 0; i < MESSAGE_COUNT; i++) {
            clientA.send(JSON.stringify({
                user: 'A',
                batch_id: i,
                changes: [{ type: 'set_property', node: '/root/Node', property: 'test', value: i }]
            }));
        }
        
        setTimeout(() => {
            if (receivedCount !== MESSAGE_COUNT) {
                reject(`Only received ${receivedCount}/${MESSAGE_COUNT} messages`);
            }
        }, 2000);
    });
}

// Run all tests
async function runTests() {
    try {
        await test1_connectClients();
        testsPassed++;
        
        await test2_forwardingAtoB();
        testsPassed++;
        
        await test3_forwardingBtoA();
        testsPassed++;
        
        await test4_noEcho();
        testsPassed++;
        
        await test5_thirdClientRejection();
        testsPassed++;
        
        await test6_rapidMessages();
        testsPassed++;
        
        console.log('\n=== Test Results ===');
        console.log(`✓ Passed: ${testsPassed}`);
        console.log(`✗ Failed: ${testsFailed}`);
        console.log(`\n✓ All tests passed! Relay server is working correctly.`);
        
    } catch (error) {
        testsFailed++;
        console.error(`\n✗ Test failed: ${error}`);
        console.log('\n=== Test Results ===');
        console.log(`✓ Passed: ${testsPassed}`);
        console.log(`✗ Failed: ${testsFailed}`);
    } finally {
        // Cleanup
        if (clientA) clientA.close();
        if (clientB) clientB.close();
        
        // Give time for cleanup
        setTimeout(() => {
            console.log('\nTest completed.');
            process.exit(testsFailed === 0 ? 0 : 1);
        }, 500);
    }
}

// Start tests
console.log(`Connecting to: ${SERVER_URL}\n`);
runTests();
