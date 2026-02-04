#!/bin/bash
set -euo pipefail

# SuperDeck Web Client — Build & Deploy Script
#
# Usage:
#   ./deploy_webclient.sh                  Build to src/WebClient/dist/
#   ./deploy_webclient.sh --serve          Build and serve with preview server
#   ./deploy_webclient.sh --target DIR     Build and copy output to DIR
#
# Prerequisites:
#   - Node.js 18+ and npm
#   - Run from the repository root

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
WEBCLIENT_DIR="$SCRIPT_DIR/src/WebClient"

if [ ! -d "$WEBCLIENT_DIR" ]; then
  echo "Error: WebClient directory not found at $WEBCLIENT_DIR"
  exit 1
fi

cd "$WEBCLIENT_DIR"

# Install dependencies if needed
if [ ! -d "node_modules" ]; then
  echo "Installing dependencies..."
  npm install
fi

# Parse arguments
SERVE=false
TARGET_DIR=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --serve)
      SERVE=true
      shift
      ;;
    --target)
      TARGET_DIR="$2"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1"
      echo "Usage: $0 [--serve] [--target DIR]"
      exit 1
      ;;
  esac
done

# Build
echo "Building web client..."
npx vite build

echo ""
echo "Build complete: $WEBCLIENT_DIR/dist/"
ls -lh dist/
echo ""

# Copy to target if specified
if [ -n "$TARGET_DIR" ]; then
  mkdir -p "$TARGET_DIR"
  cp -r dist/* "$TARGET_DIR/"
  echo "Copied build output to $TARGET_DIR/"
fi

# Serve if requested
if [ "$SERVE" = true ]; then
  echo "Starting preview server..."
  npx vite preview --port 4173
fi
