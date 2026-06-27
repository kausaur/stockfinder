import React, { useState, useCallback } from 'react';
import { ScrollView, View, Text, StyleSheet, RefreshControl, ActivityIndicator } from 'react-native';
import { useRouter } from 'expo-router';
import { api } from '../../services/api';
import { useApi } from '../../hooks/useApi';
import { colors } from '../../theme/colors';
import { StockRow } from '../../components/StockRow';
import { AlertRow } from '../../components/AlertRow';
import { SectorBar } from '../../components/SectorBar';

export default function DashboardScreen() {
    const router = useRouter();
    const [refreshing, setRefreshing] = useState(false);
    const { data, loading, error, refetch, isOfflineData } = useApi(api.getDashboard, 'dashboard_data');

    const onRefresh = useCallback(async () => {
        setRefreshing(true);
        await refetch();
        setRefreshing(false);
    }, [refetch]);

    if (loading && !refreshing && !data) {
        return (
            <View style={styles.center}>
                <ActivityIndicator size="large" color={colors.primary} />
            </View>
        );
    }

    if (error && !data) {
        return (
            <View style={styles.center}>
                <Text style={styles.errorText}>{error}</Text>
            </View>
        );
    }

    return (
        <ScrollView 
            style={styles.container}
            refreshControl={
                <RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor={colors.primary} />
            }
        >
            <View style={styles.statsCard}>
                <View style={styles.statBox}>
                    <Text style={styles.statLabel}>Total Stocks</Text>
                    <Text style={styles.statVal}>{data?.totalStocks || 0}</Text>
                </View>
                <View style={styles.statBox}>
                    <Text style={styles.statLabel}>Active Alerts</Text>
                    <Text style={[styles.statVal, { color: colors.primary }]}>{data?.alertCount || 0}</Text>
                </View>
            </View>

            <View style={styles.section}>
                <Text style={styles.sectionTitle}>Top Gainers</Text>
                {data?.topGainers?.map(stock => (
                    <StockRow key={stock.id} stock={stock} onPress={() => router.push(`/stock/${stock.id}`)} />
                ))}
            </View>

            <View style={styles.section}>
                <Text style={styles.sectionTitle}>Recent Alerts</Text>
                {data?.latestAlerts?.slice(0, 3).map(alert => (
                    <AlertRow key={alert.stockId} alert={alert} onPress={() => router.push(`/stock/${alert.stockId}`)} />
                ))}
            </View>

            <View style={styles.section}>
                <Text style={styles.sectionTitle}>Sector Performance</Text>
                <View style={styles.sectorCard}>
                    {data?.sectorPerformance?.map(sector => (
                        <SectorBar key={sector.sector} sector={sector.sector} changePercent={sector.averageChangePercent} />
                    ))}
                </View>
            </View>
        </ScrollView>
    );
}

const styles = StyleSheet.create({
    container: {
        flex: 1,
        padding: 16,
    },
    center: {
        flex: 1,
        justifyContent: 'center',
        alignItems: 'center',
    },
    errorText: {
        color: colors.danger,
        fontSize: 16,
    },
    statsCard: {
        flexDirection: 'row',
        backgroundColor: colors.card,
        borderRadius: 12,
        padding: 16,
        marginBottom: 24,
        borderWidth: 1,
        borderColor: colors.border,
    },
    statBox: {
        flex: 1,
        alignItems: 'center',
    },
    statLabel: {
        color: colors.textSecondary,
        fontSize: 14,
        marginBottom: 8,
    },
    statVal: {
        color: colors.text,
        fontSize: 24,
        fontWeight: 'bold',
    },
    section: {
        marginBottom: 24,
    },
    sectionTitle: {
        color: colors.text,
        fontSize: 18,
        fontWeight: 'bold',
        marginBottom: 12,
    },
    sectorCard: {
        backgroundColor: colors.card,
        borderRadius: 12,
        padding: 16,
        borderWidth: 1,
        borderColor: colors.border,
    }
});
