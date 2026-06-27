import React, { useState, useCallback } from 'react';
import { View, FlatList, StyleSheet, ActivityIndicator, Text } from 'react-native';
import { useRouter } from 'expo-router';
import { api } from '../../services/api';
import { useApi } from '../../hooks/useApi';
import { colors } from '../../theme/colors';
import { AlertRow } from '../../components/AlertRow';

export default function AlertsScreen() {
    const router = useRouter();
    const { data: alerts, loading, error, refetch } = useApi(api.getAlerts, 'all_alerts');
    const [refreshing, setRefreshing] = useState(false);

    const onRefresh = useCallback(async () => {
        setRefreshing(true);
        await refetch();
        setRefreshing(false);
    }, [refetch]);

    if (loading && !refreshing && !alerts) {
        return (
            <View style={styles.center}>
                <ActivityIndicator size="large" color={colors.primary} />
            </View>
        );
    }

    if (error && !alerts) {
        return (
            <View style={styles.center}>
                <Text style={{ color: colors.danger }}>{error}</Text>
            </View>
        );
    }

    return (
        <View style={styles.container}>
            <FlatList
                data={alerts || []}
                keyExtractor={(item) => item.stockId.toString()}
                renderItem={({ item }) => (
                    <AlertRow alert={item} onPress={() => router.push(`/stock/${item.stockId}`)} />
                )}
                refreshing={refreshing}
                onRefresh={onRefresh}
                contentContainerStyle={styles.listContent}
                ListEmptyComponent={
                    <View style={styles.center}>
                        <Text style={{ color: colors.textSecondary }}>No active alerts generated recently.</Text>
                    </View>
                }
            />
        </View>
    );
}

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: colors.background,
    },
    center: {
        flex: 1,
        justifyContent: 'center',
        alignItems: 'center',
        padding: 24,
    },
    listContent: {
        padding: 16,
    }
});
