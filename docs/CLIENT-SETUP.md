# SuperDeck Client Setup Guide

How to set up a dedicated machine where any user can SSH in and play SuperDeck by typing `superdeck`.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Server Requirements](#server-requirements)
- [Install the Client](#install-the-client)
- [Configure the Wrapper Script](#configure-the-wrapper-script)
- [Test It](#test-it)
- [Optional: Auto-Launch on SSH Login](#optional-auto-launch-on-ssh-login)
- [Optional: Restrict Users to Game Only](#optional-restrict-users-to-game-only)
- [Updating the Client](#updating-the-client)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

### On the client machine

- Linux (Debian 12, Ubuntu, etc.)
- .NET 10 **runtime** (not the full SDK)

### Install .NET 10 Runtime

```bash
# Using the install script
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 10.0 --runtime dotnet

# Add to system PATH (so all users can access it)
sudo ln -s ~/.dotnet/dotnet /usr/local/bin/dotnet
```

Verify:

```bash
dotnet --info
```

### On the build machine

- .NET 10 SDK (to publish the client)
- Access to the SuperDeck source code

---

## Server Requirements

You need a running SuperDeck server that the client machine can reach. See [DEPLOYMENT.md](DEPLOYMENT.md) for server setup.

For this guide, we'll assume your server is at:

```
https://superdeck.example.com
```

Replace this with your actual server URL.

---

## Install the Client

### 1. Publish the client (on your build machine)

```bash
cd /path/to/superdeck
dotnet publish src/Client -c Release -o /tmp/superdeck-client
```

### 2. Copy to the client machine

```bash
scp -r /tmp/superdeck-client user@client-machine:/tmp/superdeck-client
```

### 3. Install on the client machine

SSH into the client machine and move the files into place:

```bash
sudo mkdir -p /opt/superdeck-client
sudo cp -r /tmp/superdeck-client/* /opt/superdeck-client/
sudo chmod -R 755 /opt/superdeck-client
rm -rf /tmp/superdeck-client
```

---

## Configure the Wrapper Script

Create `/usr/local/bin/superdeck`:

```bash
sudo tee /usr/local/bin/superdeck << 'EOF'
#!/bin/bash
exec dotnet /opt/superdeck-client/SuperDeck.Client.dll --server https://superdeck.example.com
EOF
sudo chmod +x /usr/local/bin/superdeck
```

Replace `https://superdeck.example.com` with your actual server URL.

Any user on the machine can now type `superdeck` to play.

---

## Test It

```bash
# On the client machine
superdeck
```

You should see the SuperDeck title screen followed by a Login/Register prompt. No mode selection or URL entry is needed.

### Test from another machine

```bash
ssh user@client-machine
superdeck
```

---

## Optional: Auto-Launch on SSH Login

To drop users directly into the game when they SSH in, add to their `~/.bashrc` or `~/.profile`:

```bash
# Launch SuperDeck on login
if [ -t 0 ]; then
    superdeck
    exit
fi
```

The `if [ -t 0 ]` check ensures it only runs for interactive sessions (not SCP/SFTP).

---

## Optional: Restrict Users to Game Only

To create users who can **only** play SuperDeck (no shell access):

### 1. Create a dedicated shell script

```bash
sudo tee /usr/local/bin/superdeck-shell << 'EOF'
#!/bin/bash
dotnet /opt/superdeck-client/SuperDeck.Client.dll --server https://superdeck.example.com
EOF
sudo chmod +x /usr/local/bin/superdeck-shell
```

### 2. Add it as a valid shell

```bash
echo '/usr/local/bin/superdeck-shell' | sudo tee -a /etc/shells
```

### 3. Create game-only users

```bash
sudo useradd -m -s /usr/local/bin/superdeck-shell playername
sudo passwd playername
```

When `playername` SSHes in, they go straight into SuperDeck. When they quit, the session ends. They have no shell access.

---

## Updating the Client

When a new version is available:

### On the build machine

```bash
cd /path/to/superdeck
git pull
dotnet publish src/Client -c Release -o /tmp/superdeck-client
scp -r /tmp/superdeck-client user@client-machine:/tmp/superdeck-client
```

### On the client machine

```bash
sudo rm -rf /opt/superdeck-client/*
sudo cp -r /tmp/superdeck-client/* /opt/superdeck-client/
sudo chmod -R 755 /opt/superdeck-client
rm -rf /tmp/superdeck-client
```

No service restart needed - each `superdeck` invocation runs the latest files.

---

## Alternative: Environment Variable

Instead of the `--server` flag in the wrapper script, you can use the `SUPERDECK_SERVER` environment variable. Set it system-wide:

```bash
echo 'export SUPERDECK_SERVER=https://superdeck.example.com' | sudo tee /etc/profile.d/superdeck.sh
sudo chmod +x /etc/profile.d/superdeck.sh
```

Then the wrapper script can be simplified to:

```bash
sudo tee /usr/local/bin/superdeck << 'EOF'
#!/bin/bash
exec dotnet /opt/superdeck-client/SuperDeck.Client.dll
EOF
sudo chmod +x /usr/local/bin/superdeck
```

The client picks up the server URL from the environment automatically.

---

## Troubleshooting

### "command not found: superdeck"

Check the wrapper script exists and is executable:

```bash
ls -la /usr/local/bin/superdeck
```

Verify `/usr/local/bin` is in your PATH:

```bash
echo $PATH
```

### "command not found: dotnet"

The .NET runtime isn't in the system PATH. Create a symlink:

```bash
# Find where dotnet is installed
find / -name dotnet -type f 2>/dev/null

# Symlink it (adjust path as needed)
sudo ln -s /home/youruser/.dotnet/dotnet /usr/local/bin/dotnet
```

### "Failed to connect to server"

- Verify the server is running and reachable from the client machine:
  ```bash
  curl https://superdeck.example.com/api/health
  ```
- Check the URL in the wrapper script is correct
- Verify DNS resolves: `nslookup superdeck.example.com`
- Check firewall rules allow outbound HTTPS

### Terminal rendering issues

SuperDeck uses Spectre.Console which requires a terminal that supports ANSI escape codes. Most modern terminals work. If you see garbled output:

- Ensure `TERM` is set: `echo $TERM` (should be `xterm-256color` or similar)
- Try: `export TERM=xterm-256color`

### Game crashes on launch

Check that all files were copied:

```bash
ls /opt/superdeck-client/SuperDeck.Client.dll
```

Verify the .NET runtime version matches:

```bash
dotnet --list-runtimes
```

The client requires `Microsoft.NETCore.App 10.0.x`.
