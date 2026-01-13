# 🚢 Deployment & Environment Setup Guide

Complete guide untuk deployment di berbagai environment (Development, Staging, Production)

## Environment Types

| Environment | Purpose | Domain | Database |
|-------------|---------|--------|----------|
| **Development** | Local development | localhost | Local Oracle XE |
| **Staging** | Pre-production testing | staging.pelindo.local | Staging Oracle |
| **Production** | Live system | pelindo.com | Production Oracle |

---

## Development Environment Setup

### Prerequisites
- Node.js v18+
- .NET 8 SDK
- Oracle XE installed locally
- Visual Studio Code or IDE

### Configuration

**Backend (appsettings.Development.json)**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Debug"
    }
  },
  "ConnectionStrings": {
    "OracleConnection": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XE)));User Id=system;Password=bima2005"
  },
  "JwtSettings": {
    "SecretKey": "DevSecretKey2026",
    "ExpirationMinutes": 480
  },
  "CorsSettings": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:5173",
      "http://localhost:4200"
    ]
  }
}
```

**Run Development**:
```bash
# Terminal 1 - Backend
cd backend/PelindoCarLoan.API
dotnet watch run

# Terminal 2 - Frontend
cd frontend
npm run dev
```

---

## Staging Environment Setup

### Prerequisites
- Windows Server 2019/2022 or Linux Server
- .NET Runtime 8.0
- Oracle Database 19c
- IIS (Windows) or Apache/Nginx (Linux)

### Database Setup

```bash
# 1. Backup production
exp system/password@ORCL file=pelindo_backup_$(date +%Y%m%d).dmp

# 2. Import to staging
imp system/password@STAGING_DB file=pelindo_backup_20260113.dmp

# 3. Verify schema
sqlplus system/password@STAGING_DB
SQL> SELECT COUNT(*) FROM user_tables;
```

### Configuration Files

**appsettings.json** (Staging):
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "OracleConnection": "Data Source=STAGING_DB_SERVER;User Id=pelindo_user;Password=StagingPassword123"
  },
  "JwtSettings": {
    "SecretKey": "StagingSecretKeyChangeMe2026",
    "Issuer": "PelindoCarLoan.API.Staging",
    "ExpirationMinutes": 480
  },
  "CorsSettings": {
    "AllowedOrigins": [
      "https://staging.pelindo.com",
      "https://staging.pelindo.local"
    ]
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUsername": "staging@pelindo.com",
    "SmtpPassword": "staging-app-password",
    "FromEmail": "staging@pelindo.com",
    "FromName": "Pelindo Car Loan System (Staging)"
  }
}
```

### Deploy to IIS (Windows)

```bash
# 1. Build Release
cd backend/PelindoCarLoan.API
dotnet publish -c Release -o .\publish

# 2. Create IIS App Pool
# - Open IIS Manager
# - Create new App Pool: "PelindoCarLoanStaging"
# - .NET CLR Version: No Managed Code
# - Managed Pipeline Mode: Integrated

# 3. Create IIS Website
# - Physical Path: C:\inetpub\pelindo-car-loan-staging
# - Binding: https://staging.pelindo.local:443

# 4. Copy published files
xcopy .\publish\* C:\inetpub\pelindo-car-loan-staging\ /E /I /Y

# 5. Configure permissions
# - Right-click folder → Properties → Security
# - Add "IIS AppPool\PelindoCarLoanStaging" with read/write permissions

# 6. Setup SSL Certificate
# - Import certificate in IIS
# - Bind to https://staging.pelindo.local
```

### Deploy to Linux

```bash
# 1. Build Release
cd backend/PelindoCarLoan.API
dotnet publish -c Release -o ./publish

# 2. Copy to server
scp -r publish/* user@staging-server:/var/www/pelindo-car-loan/

# 3. Create systemd service
sudo nano /etc/systemd/system/pelindo-car-loan.service
```

**File: /etc/systemd/system/pelindo-car-loan.service**:
```ini
[Unit]
Description=Pelindo Car Loan API - Staging
After=network.target

[Service]
Type=notify
User=www-data
WorkingDirectory=/var/www/pelindo-car-loan
ExecStart=/usr/bin/dotnet /var/www/pelindo-car-loan/PelindoCarLoan.API.dll
Restart=on-failure
RestartSec=10

[Install]
WantedBy=multi-user.target
```

```bash
# 4. Enable and start service
sudo systemctl daemon-reload
sudo systemctl enable pelindo-car-loan
sudo systemctl start pelindo-car-loan
sudo systemctl status pelindo-car-loan

# 5. Check logs
sudo journalctl -u pelindo-car-loan -f
```

### Frontend Build & Deploy (Staging)

```bash
# 1. Build
cd frontend
npm run build

# 2. Deploy to web server
scp -r dist/* user@staging-server:/var/www/pelindo-car-loan-ui/

# 3. Configure Nginx
sudo nano /etc/nginx/sites-available/staging.pelindo.com
```

**Nginx Config**:
```nginx
server {
    listen 443 ssl http2;
    server_name staging.pelindo.com;

    ssl_certificate /etc/ssl/certs/staging.pelindo.com.crt;
    ssl_certificate_key /etc/ssl/private/staging.pelindo.com.key;

    root /var/www/pelindo-car-loan-ui;
    index index.html;

    # SPA routing
    location / {
        try_files $uri $uri/ /index.html;
    }

    # API proxy
    location /api {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Static files caching
    location ~* \.(js|css|png|jpg|gif|ico|svg|woff|woff2|ttf|eot)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
```

---

## Production Environment Setup

### High Availability Architecture

```
┌─────────────────┐
│   Load Balancer │ (Nginx/HAProxy)
│   (SSL/TLS)     │
└────────┬────────┘
         │
    ┌────┴──────┐
    │            │
┌───▼──┐    ┌───▼──┐
│App 1 │    │App 2 │ (Multiple instances)
└───┬──┘    └───┬──┘
    │            │
    └────┬───────┘
         │
    ┌────▼──────────┐
    │ Oracle DB     │
    │ Cluster/RAC   │
    └───────────────┘
```

### Prerequisites
- Windows Server 2022 or Linux Server (CentOS/Ubuntu 20+)
- .NET Runtime 8.0
- Oracle Database 19c Enterprise Edition
- Redis (for caching/sessions)
- Nginx/HAProxy (for load balancing)
- SSL/TLS Certificates

### Database Setup (Production)

```bash
# 1. Create dedicated database user
sqlplus / as sysdba
SQL> CREATE TABLESPACE pelindo_data DATAFILE '/u01/oradata/pelindo_data.dbf' SIZE 5G;
SQL> CREATE TABLESPACE pelindo_undo DATAFILE '/u01/oradata/pelindo_undo.dbf' SIZE 1G;
SQL> CREATE USER pelindo_prod IDENTIFIED BY "ProductionPassword123!@#" DEFAULT TABLESPACE pelindo_data QUOTA UNLIMITED ON pelindo_data;
SQL> GRANT CONNECT, RESOURCE, CREATE VIEW TO pelindo_prod;

# 2. Import schema
sqlplus pelindo_prod/ProductionPassword123!@# @database/CreateDatabase.sql

# 3. Create backups
RMAN> CONFIGURE BACKUP OPTIMIZATION ON;
RMAN> CONFIGURE CONTROLFILE AUTOBACKUP ON;
RMAN> BACKUP FULL DATABASE;
```

### Configuration (Production)

**appsettings.json** (Production):
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "ConnectionStrings": {
    "OracleConnection": "Data Source=PROD_DB;User Id=pelindo_prod;Password=CHANGE_ME;Max Pool Size=100;Incr Pool Size=10;Decr Pool Size=2;"
  },
  "JwtSettings": {
    "SecretKey": "CHANGE_ME_RANDOM_64_CHAR_SECRET_KEY_1234567890!@#$%^&*()",
    "Issuer": "PelindoCarLoan.API",
    "Audience": "PelindoCarLoan.Client",
    "ExpirationMinutes": 480
  },
  "CorsSettings": {
    "AllowedOrigins": [
      "https://pelindo.com",
      "https://www.pelindo.com"
    ]
  },
  "Email": {
    "SmtpHost": "smtp.office365.com",
    "SmtpPort": "587",
    "SmtpUsername": "noreply@pelindo.com",
    "SmtpPassword": "CHANGE_ME",
    "FromEmail": "noreply@pelindo.com",
    "FromName": "Pelindo Car Loan System"
  },
  "Security": {
    "RequireHttps": true,
    "EnableHsts": true,
    "HstsMaxAge": 31536000
  }
}
```

### Deploy to Production (Linux)

```bash
# 1. Build Release
cd backend/PelindoCarLoan.API
dotnet publish -c Release --self-contained -r linux-x64

# 2. Copy to production
scp -r publish/* deployer@prod-server:/opt/pelindo-car-loan/

# 3. Setup service account
sudo useradd -r -s /bin/bash pelindo
sudo chown -R pelindo:pelindo /opt/pelindo-car-loan

# 4. Create systemd service (multiple instances)
for i in {1..3}; do
  sudo nano /etc/systemd/system/pelindo-car-loan-$i.service
done
```

**systemd service with environment**:
```ini
[Unit]
Description=Pelindo Car Loan API - Instance 1
After=network-online.target
Wants=network-online.target

[Service]
Type=notify
User=pelindo
Group=pelindo
WorkingDirectory=/opt/pelindo-car-loan
Environment="ASPNETCORE_ENVIRONMENT=Production"
Environment="ASPNETCORE_URLS=http://localhost:5001"
ExecStart=/opt/pelindo-car-loan/PelindoCarLoan.API

# Restart policy
Restart=always
RestartSec=10
StartLimitInterval=60
StartLimitBurst=5

# Resource limits
MemoryLimit=2G
CPUQuota=50%

# Security
NoNewPrivileges=true
PrivateTmp=true

[Install]
WantedBy=multi-user.target
```

### Frontend Production Build

```bash
# 1. Build with optimization
cd frontend
npm run build

# 2. Deploy
scp -r dist/* deployer@prod-server:/var/www/pelindo-car-loan/

# 3. Configure Nginx (load balanced)
sudo nano /etc/nginx/nginx.conf
```

**Nginx with load balancing**:
```nginx
upstream backend {
    least_conn;
    server localhost:5001;
    server localhost:5002;
    server localhost:5003;
    keepalive 32;
}

server {
    listen 443 ssl http2;
    server_name pelindo.com www.pelindo.com;

    # SSL Configuration
    ssl_certificate /etc/ssl/certs/pelindo.com.crt;
    ssl_certificate_key /etc/ssl/private/pelindo.com.key;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;
    ssl_session_cache shared:SSL:10m;
    ssl_session_timeout 10m;

    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-Frame-Options "DENY" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;

    # Root untuk frontend
    root /var/www/pelindo-car-loan;
    index index.html;

    # SPA routing
    location / {
        try_files $uri $uri/ /index.html;
    }

    # API proxy dengan load balancing
    location /api {
        proxy_pass http://backend;
        proxy_http_version 1.1;
        proxy_set_header Connection "";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }

    # Caching static assets
    location ~* \.(js|css|png|jpg|gif|ico|svg|woff|woff2|ttf|eot)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }

    # Disable caching for index.html
    location = /index.html {
        expires -1;
        add_header Cache-Control "no-cache, no-store, must-revalidate";
    }

    # Health check endpoint
    location /health {
        proxy_pass http://backend/api/health;
        access_log off;
    }
}

# HTTP redirect to HTTPS
server {
    listen 80;
    server_name pelindo.com www.pelindo.com;
    return 301 https://$server_name$request_uri;
}
```

### Monitoring & Logging (Production)

**Setup Serilog for centralized logging**:

```csharp
// Update Program.cs
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentUserName()
    .Enrich.WithProperty("Application", "PelindoCarLoan.API")
    .WriteTo.Console()
    .WriteTo.File(
        "logs/pelindo-car-loan-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq("http://seq.pelindo.local:5341")  // Centralized logging
    .MinimumLevel.Information()
    .CreateLogger();
```

### Health Check Setup

```bash
# Add endpoint untuk health checks
# Used by load balancer untuk determine instance health
GET /api/health

# Response:
# {
#   "status": "healthy",
#   "timestamp": "2026-01-13T15:30:00Z",
#   "database": "connected",
#   "cacheStore": "connected"
# }
```

### Database Backup (Production)

```bash
# Daily backup script
#!/bin/bash
BACKUP_DIR="/backups/pelindo-car-loan"
DATE=$(date +%Y%m%d_%H%M%S)

# Backup database
expdp system/password@PROD_DB \
  DIRECTORY=backup_dir \
  DUMPFILE=pelindo_backup_$DATE.dmp \
  LOGFILE=pelindo_backup_$DATE.log

# Backup to S3
aws s3 cp $BACKUP_DIR/pelindo_backup_$DATE.dmp \
  s3://pelindo-backups/database/

# Keep last 30 days only
find $BACKUP_DIR -name "*.dmp" -mtime +30 -delete
```

Add to crontab:
```
0 2 * * * /opt/scripts/backup-database.sh >> /var/log/backup.log 2>&1
```

### Monitoring Setup

Install monitoring tools:

```bash
# Option 1: Prometheus + Grafana
docker run -d -p 9090:9090 prom/prometheus
docker run -d -p 3000:3000 grafana/grafana

# Option 2: Datadog
# - Install agent
# - Configure APM tracing
# - Setup alerts

# Option 3: Application Insights (Azure)
# - Add NuGet package: Microsoft.ApplicationInsights
# - Configure in appsettings.json
```

---

## Deployment Checklist

### Before Deployment
- [ ] Code review completed
- [ ] All tests passing
- [ ] Security scan passed
- [ ] Database backups taken
- [ ] Configuration files reviewed
- [ ] SSL certificates valid
- [ ] Load balancer configured
- [ ] Monitoring alerts setup

### During Deployment
- [ ] Database schema migrated
- [ ] Application deployed
- [ ] Services started
- [ ] Health checks passing
- [ ] Smoke tests completed
- [ ] Logs monitored

### After Deployment
- [ ] Monitoring active
- [ ] Alerts configured
- [ ] Runbook ready
- [ ] Rollback plan tested
- [ ] Performance baseline established
- [ ] User notifications sent

---

## Rollback Procedure

```bash
# If deployment fails:

# 1. Revert database
RMAN> FLASHBACK DATABASE TO TIMESTAMP 'current_timestamp - 30 minutes';
ALTER DATABASE OPEN RESETLOGS;

# 2. Revert application
systemctl stop pelindo-car-loan-{1..3}
cp -r /opt/pelindo-car-loan-backup/* /opt/pelindo-car-loan/
systemctl start pelindo-car-loan-{1..3}

# 3. Verify health
curl https://pelindo.com/api/health
curl https://pelindo.com/

# 4. Monitor
tail -f /var/log/pelindo-car-loan.log
```

---

## Performance Tuning (Production)

```csharp
// In Startup/Program.cs

// 1. Add response caching
services.AddResponseCaching();
app.UseResponseCaching();

// 2. Add compression
services.AddResponseCompression(options => {
    options.Providers.Add<GzipCompressionProvider>();
});

// 3. Configure connection pooling
// In appsettings.json
"ConnectionStrings": {
    "OracleConnection": "...;Max Pool Size=100;Incr Pool Size=10;"
}

// 4. Add Redis caching
services.AddStackExchangeRedisCache(options => {
    options.Configuration = Configuration["Redis:Connection"];
});
```

---

## Version

Last Updated: January 13, 2026
