# Deployment Guide

This guide walks through deploying the Nifty50 Stock Finder application to production for free using **Render** and **Neon**.

The application is split into two components that must be deployed separately:
1. **.NET Backend API** (Docker Service)
2. **React Web UI** (Static Site)

---

## 1. Database Setup (Neon)
Before deploying the API, you need a PostgreSQL database. 
1. Create a free account at [Neon.tech](https://neon.tech).
2. Create a new PostgreSQL project.
3. Copy the provided connection string. Note that Neon provides a URL format (e.g., `postgresql://user:pass@host/db`).
4. **Important**: Convert this URL into a standard .NET connection string format. It should look like this:
   `Host=<your-neon-host>;Database=<db-name>;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true;`

---

## 2. Deploying the Backend API (Render)
The backend is a .NET 10 API. We deploy it on Render using a Docker container.

1. Go to your [Render Dashboard](https://dashboard.render.com/) and click **New > Web Service**.
2. Connect your GitHub repository (`stockfinder`).
3. Fill out the configuration:
   - **Name**: `stockfinder-api` (or your preference)
   - **Branch**: `main`
   - **Language**: **Docker** *(This is critical! It tells Render to use our Dockerfile)*
   - **Root Directory**: *(Leave blank)*
4. Scroll down to **Advanced** and click **Add Environment Variable**. Add these exactly:
   - **Key:** `ConnectionStrings__DefaultConnection` 
     **Value:** *(Your ADO.NET formatted Neon connection string from Step 1)*
   - **Key:** `GNewsApiKey`
     **Value:** *(Your GNews API key)*
5. Click **Create Web Service**. 

Render will build the Docker container. Upon startup, the API will automatically run Entity Framework migrations to build your database tables and seed initial data.

**Once deployed, grab your API URL** (e.g., `https://stockfinder-api.onrender.com`). You will need this for the frontend!

*(Optional)* You can visit `https://<your-api-url>/swagger` to view and test your live API endpoints.

---

## 3. Deploying the Web UI (Render)
The frontend is a React application built with Vite. We deploy this on Render as a lightweight Static Site.

1. In the [Render Dashboard](https://dashboard.render.com/), click **New > Static Site**.
2. Connect the same `stockfinder` GitHub repository.
3. Fill out the configuration:
   - **Name**: `stockfinder-ui`
   - **Branch**: `main`
   - **Root Directory**: `src/Nifty50.Web` *(Crucial: This tells Render where the React app lives)*
   - **Build Command**: `npm install && npm run build`
   - **Publish Directory**: `dist`
4. Scroll down to **Advanced** and click **Add Environment Variable**:
   - **Key:** `VITE_API_BASE_URL`
   - **Value:** `https://<your-api-render-url>/api` *(Make sure to append `/api` to the URL you copied from Step 2)*
5. Click **Create Static Site**.

Render will install the dependencies, build the React app, and serve the `dist` folder. 

🎉 **You're all set!** Visit your new UI URL to see your live Stock Finder application!
