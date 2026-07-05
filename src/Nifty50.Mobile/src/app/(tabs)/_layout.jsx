import { Tabs } from 'expo-router';
import { colors } from '../../theme/colors';
import { Ionicons } from '@expo/vector-icons';

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
                    tabBarIcon: ({ color, size }) => <Ionicons name="home" size={size} color={color} />
                }}
            />
            <Tabs.Screen
                name="stocks"
                options={{
                    title: 'All Stocks',
                    tabBarLabel: 'Stocks',
                    tabBarIcon: ({ color, size }) => <Ionicons name="bar-chart" size={size} color={color} />
                }}
            />
            <Tabs.Screen
                name="alerts"
                options={{
                    title: 'Alerts',
                    tabBarLabel: 'Alerts',
                    tabBarIcon: ({ color, size }) => <Ionicons name="notifications" size={size} color={color} />
                }}
            />
            <Tabs.Screen
                name="recommendations"
                options={{
                    title: 'Picks',
                    headerShown: false,
                    tabBarLabel: 'Picks',
                    tabBarIcon: ({ color, size }) => <Ionicons name="star" size={size} color={color} />
                }}
            />
            <Tabs.Screen
                name="screener"
                options={{
                    title: 'Screener',
                    headerShown: false,
                    tabBarLabel: 'Screener',
                    tabBarIcon: ({ color, size }) => <Ionicons name="search" size={size} color={color} />
                }}
            />
            <Tabs.Screen
                name="settings"
                options={{
                    title: 'Settings',
                    tabBarLabel: 'Settings',
                    tabBarIcon: ({ color, size }) => <Ionicons name="settings" size={size} color={color} />
                }}
            />
        </Tabs>
    );
}
