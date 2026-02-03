#!/bin/bash
set -e

STAGING_DIR="/tmp/superdeck-client"
CLIENT_DIR="/opt/superdeck-client"
WRAPPER="/usr/local/bin/superdeck"

echo "=== SuperDeck Client Update ==="

# Check for staged build
if [ ! -d "$STAGING_DIR" ]; then
    echo "ERROR: No staged build found at $STAGING_DIR"
    echo ""
    echo "On the build machine, run:"
    echo "  dotnet publish src/Client -c Release -o /tmp/superdeck-client"
    echo "  scp -r /tmp/superdeck-client $(whoami)@$(hostname):/tmp/superdeck-client"
    exit 1
fi

# Deploy
echo "Deploying to $CLIENT_DIR..."
mkdir -p "$CLIENT_DIR"
cp -r "$STAGING_DIR"/* "$CLIENT_DIR/"
chmod -R 755 "$CLIENT_DIR"
rm -rf "$STAGING_DIR"

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
    echo "NOTE: No wrapper script at $WRAPPER — 'superdeck' command won't work until one is created"
fi
