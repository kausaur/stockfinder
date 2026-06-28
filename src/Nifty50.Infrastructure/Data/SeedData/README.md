# Data Seed Files

This directory contains curated JSON files used to seed and update the Long-Term Investment Recommendation Engine.

## Monthly Update Process

1. **Update `quality_metrics.json`**: Add or modify holdings data (Promoter, FII, DII) and pledge percentages based on latest NSE shareholding pattern filings.
2. **Update `sector_benchmarks.json`**: Update median P/E, ROE, etc., if sector valuations have shifted significantly.
3. **Update `index_memberships.json`**: If Nifty50 constitutes change, add or remove symbols here.
4. **Trigger Update**: Hit `POST /api/admin/refresh-quality` to re-seed from these updated files. The analysis engine will auto-recalculate on the next refresh cycle.
