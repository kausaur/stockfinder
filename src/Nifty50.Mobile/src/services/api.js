import axios from 'axios';
import { Platform } from 'react-native';

// Use production API if deployed, otherwise fallback to local dev
// You might need to change localhost to your dev machine IP (e.g. 192.168.x.x) for physical device testing
const API_BASE_URL = process.env.EXPO_PUBLIC_API_BASE_URL ?? 'http://10.0.2.2:5276/api';

const apiClient = axios.create({
    baseURL: API_BASE_URL,
    timeout: 15000,
});

export const api = {
    getDashboard: () => apiClient.get('/dashboard'),
    getStocks: (search = '', sector = '') => apiClient.get('/stocks', { params: { search, sector } }),
    getStock: (id) => apiClient.get(`/stocks/${id}`),
    getStockPrices: (id, from, to) => apiClient.get(`/stocks/${id}/prices`, { params: { from, to } }),
    getAnalysis: (id) => apiClient.get(`/stocks/${id}/analysis`),
    getTechnical: (id) => apiClient.get(`/stocks/${id}/technicals`),
    getFundamental: (id) => apiClient.get(`/stocks/${id}/fundamentals`),
    getSentiment: (id) => apiClient.get(`/stocks/${id}/sentiment`),
    getAlerts: () => apiClient.get('/alerts'),
    getAdminDashboard: () => apiClient.get('/admin/health'),
    triggerRefresh: () => apiClient.post('/refresh'),
    getScoringProfiles: () => apiClient.get('/scoring-profiles'),
    getActiveProfile: () => apiClient.get('/scoring-profiles/active'),
    activateProfile: (id) => apiClient.post(`/scoring-profiles/${id}/activate`),
    getRecommendations: () => apiClient.get('/recommendations/dashboard'),
    getScreenerResults: (filters) => apiClient.post('/recommendations/screener', filters),
    getPeerComparison: (stockId) => apiClient.get(`/recommendations/${stockId}/peers`),
    registerDevice: (token, deviceName, platform) => apiClient.post('/devices', { expoPushToken: token, deviceName, platform }),
};
