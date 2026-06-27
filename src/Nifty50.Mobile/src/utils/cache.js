import AsyncStorage from '@react-native-async-storage/async-storage';

export const cacheData = async (key, data) => {
    try {
        const payload = {
            timestamp: Date.now(),
            data,
        };
        await AsyncStorage.setItem(key, JSON.stringify(payload));
    } catch (error) {
        console.error('Error caching data:', error);
    }
};

export const getCachedData = async (key) => {
    try {
        const value = await AsyncStorage.getItem(key);
        if (value !== null) {
            const payload = JSON.parse(value);
            return payload.data;
        }
    } catch (error) {
        console.error('Error retrieving cached data:', error);
    }
    return null;
};

export const getCacheInfo = async (key) => {
    try {
        const value = await AsyncStorage.getItem(key);
        if (value !== null) {
            const payload = JSON.parse(value);
            return { timestamp: payload.timestamp, data: payload.data };
        }
    } catch (error) {
        console.error('Error retrieving cache info:', error);
    }
    return null;
};
