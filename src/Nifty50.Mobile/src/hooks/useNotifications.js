import { useEffect, useState } from 'react';

// Push notifications are temporarily disabled in Expo Go to prevent SDK 53+ crashes.
// import * as Device from 'expo-device';
// import * as Notifications from 'expo-notifications';

export const useNotifications = () => {
    const [expoPushToken, setExpoPushToken] = useState('');

    useEffect(() => {
        // Disabled for Expo Go
    }, []);

    return expoPushToken;
};
