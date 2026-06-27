import { useEffect, useRef, useState } from 'react';
import * as Device from 'expo-device';
import * as Notifications from 'expo-notifications';
import { Platform } from 'react-native';
import { api } from '../services/api';
import { useRouter } from 'expo-router';

Notifications.setNotificationHandler({
    handleNotification: async () => ({
        shouldShowAlert: true,
        shouldPlaySound: true,
        shouldSetBadge: false,
    }),
});

export const useNotifications = () => {
    const [expoPushToken, setExpoPushToken] = useState('');
    const notificationListener = useRef();
    const responseListener = useRef();
    const router = useRouter();

    useEffect(() => {
        registerForPushNotificationsAsync().then(token => {
            if (token) {
                setExpoPushToken(token);
                // Send token to backend
                api.registerDevice(token, Device.deviceName || 'Unknown', Platform.OS)
                    .catch(err => console.error('Failed to register device:', err));
            }
        });

        // Handle foreground notifications
        notificationListener.current = Notifications.addNotificationReceivedListener(notification => {
            console.log('Notification received in foreground:', notification);
        });

        // Handle tap on notification
        responseListener.current = Notifications.addNotificationResponseReceivedListener(response => {
            const data = response.notification.request.content.data;
            if (data?.stockId) {
                router.push(`/stock/${data.stockId}`);
            } else {
                router.push('/alerts');
            }
        });

        return () => {
            if (notificationListener.current) Notifications.removeNotificationSubscription(notificationListener.current);
            if (responseListener.current) Notifications.removeNotificationSubscription(responseListener.current);
        };
    }, []);

    return expoPushToken;
};

async function registerForPushNotificationsAsync() {
    let token;
    
    if (Platform.OS === 'android') {
        Notifications.setNotificationChannelAsync('default', {
            name: 'default',
            importance: Notifications.AndroidImportance.MAX,
            vibrationPattern: [0, 250, 250, 250],
            lightColor: '#FF231F7C',
        });
    }

    if (Device.isDevice) {
        const { status: existingStatus } = await Notifications.getPermissionsAsync();
        let finalStatus = existingStatus;
        if (existingStatus !== 'granted') {
            const { status } = await Notifications.requestPermissionsAsync();
            finalStatus = status;
        }
        if (finalStatus !== 'granted') {
            console.log('Failed to get push token for push notification!');
            return;
        }
        token = (await Notifications.getExpoPushTokenAsync({
            projectId: 'replace-with-your-expo-project-id', // Optional if defined in app.json
        })).data;
    } else {
        console.log('Must use physical device for Push Notifications');
    }

    return token;
}
