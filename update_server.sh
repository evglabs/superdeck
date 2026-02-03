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

# Preserve production config
echo "Backing up production config..."
cp "$PUBLISH_DIR/appsettings.Production.json" /tmp/appsettings.Production.json.bak

# Rebuild
echo "Publishing server..."
dotnet publish src/Server -c Release -o "$PUBLISH_DIR"

# Restore production config
echo "Restoring production config..."
cp /tmp/appsettings.Production.json.bak "$PUBLISH_DIR/appsettings.Production.json"
rm /tmp/appsettings.Production.json.bak

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
