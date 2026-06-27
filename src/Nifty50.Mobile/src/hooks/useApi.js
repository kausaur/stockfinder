import { useState, useCallback, useEffect } from 'react';
import { useNetwork } from './useNetwork';
import { cacheData, getCachedData } from '../utils/cache';

export const useApi = (apiFunc, cacheKey = null, dependencies = []) => {
    const [data, setData] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [isOfflineData, setIsOfflineData] = useState(false);
    const isOnline = useNetwork();

    const fetchData = useCallback(async () => {
        setLoading(true);
        setError(null);
        setIsOfflineData(false);

        try {
            if (!isOnline) {
                if (cacheKey) {
                    const cached = await getCachedData(cacheKey);
                    if (cached) {
                        setData(cached);
                        setIsOfflineData(true);
                        setLoading(false);
                        return;
                    }
                }
                throw new Error('No internet connection and no cached data available.');
            }

            const response = await apiFunc();
            setData(response.data);
            
            if (cacheKey) {
                await cacheData(cacheKey, response.data);
            }
        } catch (err) {
            // If network request fails, fallback to cache if available
            if (cacheKey) {
                const cached = await getCachedData(cacheKey);
                if (cached) {
                    setData(cached);
                    setIsOfflineData(true);
                    setLoading(false);
                    return;
                }
            }
            setError(err.message || 'An error occurred');
        } finally {
            setLoading(false);
        }
    }, [isOnline, ...dependencies]);

    useEffect(() => {
        fetchData();
    }, [fetchData]);

    return { data, loading, error, refetch: fetchData, isOfflineData };
};
