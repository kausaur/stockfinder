# Local Setup Guide

Get the Nifty50 Stock Finder running on your machine from scratch.

---

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| **.NET SDK** | 8+ | Backend API |
| **Node.js** | 18+ (with npm) | Web & Mobile frontends |
| **Docker Desktop** | Latest | Local PostgreSQL database |
| **GNews API Key** | Free tier | Sentiment analysis ([gnews.io](https://gnews.io/)) |

---

## 1. Database

Start a local PostgreSQL instance with Docker:

```bash
docker run --name stockfinder-db \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  -d postgres
```

> The default connection string in `appsettings.json` already points to `localhost:5432` with user `postgres` / password `postgres`.

---

## 2. Backend API

### Configure secrets

Navigate to the API project and store your GNews key outside of source control:

```bash
cd src/Nifty50.Api
dotnet user-secrets init
dotnet user-secrets set "GNewsApiKey" "YOUR_API_KEY_HERE"
```

### Run the API

```bash
dotnet run --project src/Nifty50.Api
```

On first startup the `DataRefreshService` will:
1. Apply any pending EF Core migrations automatically.
2. Seed the six preset scoring profiles.
3. Begin fetching 8 years of historical data for all 50 stocks (this takes several minutes on the initial run).

After that, data refreshes automatically every **24 hours** (configurable — see below).

The API will be available at **http://localhost:5062** and Swagger docs at **http://localhost:5062/swagger**.

---

## 3. Web Frontend

Open a new terminal:

```bash
cd src/Nifty50.Web
npm install
npm run dev
```

Open **http://localhost:5173** in your browser.

---

## 4. Mobile App (Optional)

The Expo-based mobile app lives in `src/Nifty50.Mobile`.

```bash
cd src/Nifty50.Mobile
npm install
npm start
```

- **Physical device**: Scan the QR code with the Expo Go app.
- **Android emulator**: Press `a` in the terminal.
- **iOS simulator**: Press `i` (macOS only).

> By default the mobile app points to the production Render API. To target your local backend, update `API_BASE_URL` in `src/Nifty50.Mobile/src/services/api.js` to your machine's LAN IP (e.g. `http://192.168.x.x:5062/api`).

---

## Configuration Reference

All runtime settings live in `src/Nifty50.Api/appsettings.json`:

| Setting | Default | Description |
|---------|---------|-------------|
| `DataRefresh:IntervalHours` | `24` | Hours between automatic data refresh cycles |
| `DataRefresh:SkipIfRecentHours` | `12` | Skip refresh on startup if data was updated within this many hours (prevents wasting API quotas on Render free tier cold starts) |
| `GNews:MaxStocksPerRefresh` | `20` | Max stocks to fetch sentiment for per cycle (GNews free tier = 100 req/day) |
| `GNewsApiKey` | *(user-secrets)* | Your GNews API key |

---

## Keep-Alive for Render Free Tier

Render free instances spin down after ~15 minutes of inactivity, causing 30-60s cold starts. To keep your API permanently awake, set up a free external pinger:

### Option A: cron-job.org (Recommended)

1. Go to [cron-job.org](https://cron-job.org) and create a free account.
2. Create a new cron job:
   - **URL**: `https://YOUR-APP.onrender.com/healthz`
   - **Schedule**: Every 14 minutes
   - **Request method**: GET
3. Save. Your instance will now stay awake 24/7.

### Option B: UptimeRobot

1. Go to [UptimeRobot](https://uptimerobot.com) and create a free account.
2. Add a new monitor:
   - **Monitor Type**: HTTP(s)
   - **URL**: `https://YOUR-APP.onrender.com/healthz`
   - **Monitoring Interval**: 5 minutes
3. Save. This also gives you uptime alerts if the service goes down.

> **Note:** The `DataRefreshService` includes a smart skip — even if the instance restarts unexpectedly, it will not waste API quotas re-fetching data that was refreshed within the last 12 hours.

---

## Cloud Deployment

For deploying to production using free-tier services (Render + Neon), see the separate **[Deployment Guide](DEPLOYMENT.md)**.
