#!/bin/bash
set -e

CLIENT_DIR="/opt/superdeck-client"
WRAPPER="/usr/local/bin/superdeck"

echo "=== SuperDeck Client Deploy ==="

# Check prerequisites
if [ "$(id -u)" -ne 0 ]; then
    echo "ERROR: Must be run as root (use sudo)"
    exit 1
fi

if ! command -v git &>/dev/null; then
    echo "ERROR: git is not installed"
    exit 1
fi

if ! command -v dotnet &>/dev/null; then
    echo "ERROR: dotnet SDK is not installed"
    exit 1
fi

# Detect repo directory (where this script lives)
REPO_DIR="$(cd "$(dirname "$0")" && pwd)"
echo "Repo directory: $REPO_DIR"

# Pull latest changes
echo "Pulling latest changes..."
cd "$REPO_DIR"
git pull

# Publish client
echo "Publishing client..."
dotnet publish src/Client -c Release -o "$CLIENT_DIR"
chmod -R 755 "$CLIENT_DIR"

# Create wrapper script
echo "Creating wrapper at $WRAPPER..."
cat > "$WRAPPER" <<'EOF'
#!/bin/bash
exec dotnet /opt/superdeck-client/SuperDeck.Client.dll "$@"
EOF
chmod +x "$WRAPPER"

# Verify
if [ ! -f "$CLIENT_DIR/SuperDeck.Client.dll" ]; then
    echo "=== ERROR: Client DLL not found after publish ==="
    exit 1
fi

if [ ! -x "$WRAPPER" ]; then
    echo "=== ERROR: Wrapper script not executable ==="
    exit 1
fi

echo "=== Client deployed ==="
echo "Any user can now run: superdeck --help"
