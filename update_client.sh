#!/bin/bash
set -e

REPO_DIR="/opt/superdeck"
CLIENT_DIR="/opt/superdeck-client"
WRAPPER="/usr/local/bin/superdeck"

echo "=== SuperDeck Client Update ==="

# Pull latest changes
echo "Pulling latest changes..."
cd "$REPO_DIR"
git pull

# Rebuild
echo "Publishing client..."
dotnet publish src/Client -c Release -o /tmp/superdeck-client

# Deploy
echo "Deploying client..."
sudo cp -r /tmp/superdeck-client/* "$CLIENT_DIR/"
sudo chmod -R 755 "$CLIENT_DIR"
rm -rf /tmp/superdeck-client

# Verify
if [ -f "$CLIENT_DIR/SuperDeck.Client.dll" ]; then
    echo "=== Client updated ==="
else
    echo "=== WARNING: Client DLL not found ==="
    exit 1
fi

if [ -x "$WRAPPER" ]; then
    echo "Wrapper script OK: $WRAPPER"
else
    echo "NOTE: No wrapper script at $WRAPPER — run 'superdeck' won't work until one is created"
fi
