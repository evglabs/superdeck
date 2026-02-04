#!/bin/bash
set -e

REPO_DIR="/opt/superdeck"
PUBLISH_DIR="/opt/superdeck/publish"
SERVICE_NAME="superdeck"

echo "=== SuperDeck Server Update ==="

# Pull latest changes
echo "Pulling latest changes..."
cd "$REPO_DIR"
git pull

# Preserve production config if it exists
HAS_PROD_CONFIG=false
if [ -f "$PUBLISH_DIR/appsettings.Production.json" ]; then
    echo "Backing up production config..."
    cp "$PUBLISH_DIR/appsettings.Production.json" /tmp/appsettings.Production.json.bak
    HAS_PROD_CONFIG=true
else
    echo "No existing production config found, skipping backup."
fi

# Rebuild
echo "Publishing server..."
dotnet publish src/Server -c Release -o "$PUBLISH_DIR"

# Restore production config if we backed it up
if [ "$HAS_PROD_CONFIG" = true ]; then
    echo "Restoring production config..."
    cp /tmp/appsettings.Production.json.bak "$PUBLISH_DIR/appsettings.Production.json"
    rm /tmp/appsettings.Production.json.bak
fi

# Restart service
echo "Restarting service..."
sudo systemctl restart "$SERVICE_NAME"

# Wait a moment then check status
sleep 2
if systemctl is-active --quiet "$SERVICE_NAME"; then
    echo "=== Server updated and running ==="
else
    echo "=== WARNING: Service failed to start ==="
    sudo journalctl -u "$SERVICE_NAME" -n 20 --no-pager
    exit 1
fi
