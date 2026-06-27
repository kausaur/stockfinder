import { Stack } from 'expo-router';
import { StatusBar } from 'expo-status-bar';
import { useNotifications } from '../hooks/useNotifications';
import { OfflineBanner } from '../components/OfflineBanner';
import { colors } from '../theme/colors';

export default function RootLayout() {
    // Initialize push notifications
    useNotifications();

    return (
        <>
            <StatusBar style="light" backgroundColor={colors.background} />
            <OfflineBanner />
            <Stack
                screenOptions={{
                    headerStyle: { backgroundColor: colors.background },
                    headerTintColor: colors.text,
                    headerShadowVisible: false,
                    contentStyle: { backgroundColor: colors.background }
                }}
            >
                <Stack.Screen name="(tabs)" options={{ headerShown: false }} />
                <Stack.Screen name="stock/[id]" options={{ title: 'Stock Details' }} />
                <Stack.Screen name="admin" options={{ title: 'Admin Settings', presentation: 'modal' }} />
            </Stack>
        </>
    );
}
