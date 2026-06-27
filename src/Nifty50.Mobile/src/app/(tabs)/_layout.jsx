import { Tabs } from 'expo-router';
import { colors } from '../../theme/colors';

export default function TabLayout() {
    return (
        <Tabs
            screenOptions={{
                headerStyle: { backgroundColor: colors.background },
                headerTintColor: colors.text,
                tabBarStyle: { backgroundColor: colors.surface, borderTopColor: colors.border },
                tabBarActiveTintColor: colors.primary,
                tabBarInactiveTintColor: colors.textSecondary,
            }}
        >
            <Tabs.Screen
                name="index"
                options={{
                    title: 'Dashboard',
                    tabBarLabel: 'Home',
                }}
            />
            <Tabs.Screen
                name="stocks"
                options={{
                    title: 'All Stocks',
                    tabBarLabel: 'Stocks',
                }}
            />
            <Tabs.Screen
                name="alerts"
                options={{
                    title: 'Alerts',
                    tabBarLabel: 'Alerts',
                }}
            />
            <Tabs.Screen
                name="settings"
                options={{
                    title: 'Settings',
                    tabBarLabel: 'Settings',
                }}
            />
        </Tabs>
    );
}
