# SuperDeck Deployment Guide

This guide covers deploying SuperDeck for both development and production environments.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Development Setup](#development-setup)
- [Production Deployment](#production-deployment)
- [Database Configuration](#database-configuration)
- [Environment Variables](#environment-variables)
- [Reverse Proxy Setup](#reverse-proxy-setup)
- [Monitoring and Logging](#monitoring-and-logging)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Software

| Software | Version | Purpose |
|----------|---------|---------|
| .NET SDK | 10.0+ | Runtime and build tools |
| Git | Any | Source control |

### Optional (Production)

| Software | Version | Purpose |
|----------|---------|---------|
| MariaDB | 10.6+ | Production database |
| Nginx | 1.18+ | Reverse proxy |
| systemd | - | Service management |

### Verify Installation

```bash
# Check .NET version
dotnet --version
# Expected: 10.0.x or higher

# Check Git
git --version
```

---

## Development Setup

### 1. Clone and Build

```bash
# Clone repository
git clone <repository-url> superdeck
cd superdeck

# Build all projects
dotnet build SuperDeck.slnx
```

### 2. Run Locally (Offline Mode)

The simplest way to run SuperDeck for development:

```bash
# Use the provided script
./run_client.sh

# Or manually
dotnet run --project src/Client
```

This automatically:
- Compiles the server if needed
- Launches an embedded server on `localhost:5000`
- Connects the client to it
- Uses SQLite database (auto-created)

### 3. Run Server Separately

For development with separate server/client:

```bash
# Terminal 1: Start server
dotnet run --project src/Server

# Terminal 2: Start client
dotnet run --project src/Client
# Select "Online" mode and enter: http://localhost:5000
```

### 4. Development Database

By default, development uses SQLite:
- Database file: `src/Server/superdeck.db`
- Auto-created on first run
- No configuration needed

---

## Production Deployment

### Option A: Single Server Deployment

#### 1. Prepare the Server

```bash
# Install .NET 10 runtime (Ubuntu/Debian)
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 10.0 --runtime aspnetcore

# Or install the SDK for building on server
./dotnet-install.sh --channel 10.0
```

#### 2. Clone and Build

```bash
cd /opt
git clone <repository-url> superdeck
cd superdeck
dotnet publish src/Server -c Release -o /opt/superdeck/publish
```

#### 3. Configure for Production

Create `/opt/superdeck/publish/appsettings.Production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "DatabaseProvider": "MariaDB",
  "ConnectionStrings": {
    "MariaDB": "Server=localhost;Database=superdeck;User=superdeck_user;Password=<STRONG_PASSWORD>"
  },
  "GameSettings": {
    "Auth": {
      "UseJwt": true,
      "Jwt": {
        "Secret": "<GENERATE_A_64_CHARACTER_SECRET>",
        "Issuer": "SuperDeck",
        "Audience": "SuperDeck",
        "ExpirationMinutes": 1440
      }
    },
    "RateLimit": {
      "Enabled": true,
      "GlobalPermitLimit": 100,
      "GlobalWindowSeconds": 60
    }
  }
}
```

#### 4. Create Systemd Service

Create `/etc/systemd/system/superdeck.service`:

```ini
[Unit]
Description=SuperDeck Game Server
After=network.target mariadb.service

[Service]
Type=simple
User=www-data
Group=www-data
WorkingDirectory=/opt/superdeck/publish
# Find your dotnet path with: which dotnet
ExecStart=/usr/bin/dotnet /opt/superdeck/publish/SuperDeck.Server.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

#### 5. Enable and Start

```bash
sudo systemctl daemon-reload
sudo systemctl enable superdeck
sudo systemctl start superdeck

# Check status
sudo systemctl status superdeck

# View logs
sudo journalctl -u superdeck -f
```

### Option B: Docker Deployment

#### Dockerfile

Create `Dockerfile` in project root:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Server -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "SuperDeck.Server.dll"]
```

#### Docker Compose

Create `docker-compose.yml`:

```yaml
version: '3.8'

services:
  superdeck:
    build: .
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - DatabaseProvider=MariaDB
      - ConnectionStrings__MariaDB=Server=db;Database=superdeck;User=superdeck;Password=secretpass
    depends_on:
      - db
    restart: unless-stopped

  db:
    image: mariadb:10.11
    environment:
      - MYSQL_ROOT_PASSWORD=rootpass
      - MYSQL_DATABASE=superdeck
      - MYSQL_USER=superdeck
      - MYSQL_PASSWORD=secretpass
    volumes:
      - mariadb_data:/var/lib/mysql
    restart: unless-stopped

volumes:
  mariadb_data:
```

#### Run with Docker

```bash
docker-compose up -d
```

---

## Database Configuration

### SQLite (Development/Offline)

Default configuration - no setup needed:

```json
{
  "DatabaseProvider": "SQLite"
}
```

Database created at: `src/Server/superdeck.db`

### MariaDB (Production)

#### 1. Install MariaDB

```bash
# Ubuntu/Debian
sudo apt install mariadb-server

# Start and secure
sudo systemctl start mariadb
sudo mysql_secure_installation
```

#### 2. Create Database and User

```sql
-- Connect as root
sudo mysql -u root -p

-- Create database
CREATE DATABASE superdeck CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Create user
CREATE USER 'superdeck_user'@'localhost' IDENTIFIED BY '<STRONG_PASSWORD>';

-- Grant permissions
GRANT ALL PRIVILEGES ON superdeck.* TO 'superdeck_user'@'localhost';
FLUSH PRIVILEGES;
```

#### 3. Configure Connection

Tables are created automatically when the server starts for the first time - no manual schema setup is needed.

In `appsettings.Production.json`:

```json
{
  "DatabaseProvider": "MariaDB",
  "ConnectionStrings": {
    "MariaDB": "Server=localhost;Database=superdeck;User=superdeck_user;Password=<PASSWORD>"
  }
}
```

---

## Environment Variables (Alternative to Config Files)

If you prefer environment variables over JSON config files (common in Docker and CI/CD), you can use them **instead of** `appsettings.Production.json`. This is not needed if you already have a config file.

Use `__` (double underscore) to represent nested keys:

```bash
# These two are equivalent:
# appsettings.json:  "ConnectionStrings": { "MariaDB": "Server=..." }
# Environment var:
export ConnectionStrings__MariaDB="Server=localhost;Database=superdeck;User=superdeck;Password=secret"
```

The two variables you **do** always need as environment variables (they aren't set in config files):

| Variable | Description | Example |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Tells the server to load `appsettings.Production.json` | `Production` |
| `ASPNETCORE_URLS` | What address/port the server listens on | `http://0.0.0.0:5000` |

These are set in the systemd service file (step 4 above) or in Docker Compose.

---

## Reverse Proxy Setup

### Nginx Configuration

Create `/etc/nginx/sites-available/superdeck`:

```nginx
server {
    listen 80;
    server_name superdeck.example.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

Enable the site:

```bash
sudo ln -s /etc/nginx/sites-available/superdeck /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### HTTPS with Certbot

```bash
sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx -d superdeck.example.com
```

---

## Monitoring and Logging

### View Logs

```bash
# Systemd logs
sudo journalctl -u superdeck -f

# Last 100 lines
sudo journalctl -u superdeck -n 100

# Since today
sudo journalctl -u superdeck --since today
```

### Health Check Endpoint

The server exposes a health endpoint:

```bash
curl http://localhost:5000/api/health
# Returns: 200 OK if healthy
```

### Monitoring Script

Create `/opt/superdeck/health-check.sh`:

```bash
#!/bin/bash
if ! curl -sf http://localhost:5000/api/health > /dev/null; then
    echo "SuperDeck health check failed at $(date)" >> /var/log/superdeck-health.log
    systemctl restart superdeck
fi
```

Add to cron:

```bash
# Check every 5 minutes
*/5 * * * * /opt/superdeck/health-check.sh
```

---

## Troubleshooting

### Server Won't Start

1. **Check logs:**
   ```bash
   sudo journalctl -u superdeck -n 50 --no-pager
   ```

2. **Verify .NET installation:**
   ```bash
   dotnet --info
   ```

3. **Check permissions:**
   ```bash
   ls -la /opt/superdeck/publish/
   # Ensure www-data can read
   ```

4. **Test manually:**
   ```bash
   cd /opt/superdeck/publish
   dotnet SuperDeck.Server.dll
   ```

### Database Connection Errors

1. **Verify MariaDB is running:**
   ```bash
   sudo systemctl status mariadb
   ```

2. **Test connection:**
   ```bash
   mysql -u superdeck_user -p -e "SELECT 1"
   ```

3. **Check connection string format:**
   - Format: `Server=host;Database=db;User=user;Password=pass`

### Port Already in Use

```bash
# Find what's using port 5000
sudo lsof -i :5000

# Kill the process if needed
sudo kill -9 <PID>
```

### Card Scripts Failing

Check script timeout settings in `appsettings.json`:

```json
{
  "GameSettings": {
    "Script": {
      "TimeoutMs": 500,
      "MemoryLimitMB": 50
    }
  }
}
```

---

## Security Checklist

Before going live:

- [ ] Change default JWT secret (min 64 characters)
- [ ] Enable rate limiting
- [ ] Use HTTPS in production
- [ ] Restrict database user permissions
- [ ] Keep .NET runtime updated
- [ ] Configure firewall (only expose 80/443)
- [ ] Regular database backups
- [ ] Review log files periodically

---

## Backup and Recovery

### Database Backup (MariaDB)

```bash
# Backup
mysqldump -u root -p superdeck > backup_$(date +%Y%m%d).sql

# Restore
mysql -u root -p superdeck < backup_20240101.sql
```

### Database Backup (SQLite)

```bash
# Backup - just copy the file
cp src/Server/superdeck.db backup_$(date +%Y%m%d).db

# Restore
cp backup_20240101.db src/Server/superdeck.db
```

### Automated Backup Script

```bash
#!/bin/bash
BACKUP_DIR=/opt/backups/superdeck
DATE=$(date +%Y%m%d_%H%M%S)

mkdir -p $BACKUP_DIR
mysqldump -u superdeck_user -p'password' superdeck | gzip > $BACKUP_DIR/superdeck_$DATE.sql.gz

# Keep only last 7 days
find $BACKUP_DIR -name "*.sql.gz" -mtime +7 -delete
```
