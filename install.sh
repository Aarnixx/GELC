#!/bin/bash

# Godot Live Collaboration - Installation Script
# This script helps set up the relay server and plugin

echo "=========================================="
echo "Godot Live Collaboration - Setup Wizard"
echo "=========================================="
echo ""

# Check if Node.js is installed
if ! command -v node &> /dev/null; then
    echo "❌ Node.js is not installed!"
    echo "Please install Node.js from: https://nodejs.org/"
    exit 1
fi

echo "✓ Node.js is installed ($(node --version))"

# Check if npm is installed
if ! command -v npm &> /dev/null; then
    echo "❌ npm is not installed!"
    echo "Please install npm"
    exit 1
fi

echo "✓ npm is installed ($(npm --version))"
echo ""

# Ask what to install
echo "What would you like to set up?"
echo "1) Relay Server only"
echo "2) Test the relay server"
echo "3) Both"
read -p "Enter choice (1-3): " choice

case $choice in
    1|3)
        echo ""
        echo "Setting up Relay Server..."
        cd relay-server
        
        if [ -d "node_modules" ]; then
            echo "✓ Dependencies already installed"
        else
            echo "Installing dependencies..."
            npm install
            if [ $? -eq 0 ]; then
                echo "✓ Dependencies installed successfully"
            else
                echo "❌ Failed to install dependencies"
                exit 1
            fi
        fi
        
        echo ""
        echo "Relay server is ready!"
        echo "To start: cd relay-server && npm start"
        ;;
esac

case $choice in
    2|3)
        echo ""
        echo "Running relay server tests..."
        cd relay-server
        
        # Check if server is running
        if lsof -Pi :8080 -sTCP:LISTEN -t >/dev/null ; then
            echo "✓ Relay server is running on port 8080"
            echo "Running tests..."
            node test-relay.js
        else
            echo "⚠ Relay server is not running!"
            echo "Please start the server first:"
            echo "  cd relay-server && npm start"
            echo ""
            echo "Then run tests with:"
            echo "  cd relay-server && node test-relay.js"
        fi
        ;;
esac

echo ""
echo "=========================================="
echo "Next Steps:"
echo "=========================================="
echo ""
echo "1. Start the relay server:"
echo "   cd relay-server"
echo "   npm start"
echo ""
echo "2. Copy plugin to your Godot project:"
echo "   cp -r addons/live_collab your_project/addons/"
echo ""
echo "3. Configure user IDs in LiveCollabPlugin.cs:"
echo "   Machine A: private string _userId = \"A\";"
echo "   Machine B: private string _userId = \"B\";"
echo ""
echo "4. Enable plugin in Godot:"
echo "   Project → Project Settings → Plugins"
echo ""
echo "5. Start collaborating!"
echo ""
echo "For detailed instructions, see:"
echo "  docs/QUICKSTART.md"
echo "  docs/README.md"
echo ""
echo "=========================================="
