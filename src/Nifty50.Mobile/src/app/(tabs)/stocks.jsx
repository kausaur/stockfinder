import React, { useState, useCallback } from 'react';
import { View, FlatList, TextInput, StyleSheet, ActivityIndicator } from 'react-native';
import { useRouter } from 'expo-router';
import { api } from '../../services/api';
import { useApi } from '../../hooks/useApi';
import { colors } from '../../theme/colors';
import { StockRow } from '../../components/StockRow';

export default function StocksScreen() {
    const router = useRouter();
    const [searchQuery, setSearchQuery] = useState('');
    const { data: stocks, loading, refetch } = useApi(() => api.getStocks(searchQuery), `stocks_list_${searchQuery}`, [searchQuery]);
    const [refreshing, setRefreshing] = useState(false);

    const onRefresh = useCallback(async () => {
        setRefreshing(true);
        await refetch();
        setRefreshing(false);
    }, [refetch]);

    const renderHeader = () => (
        <View style={styles.header}>
            <TextInput
                style={styles.searchInput}
                placeholder="Search stocks by symbol or name..."
                placeholderTextColor={colors.textSecondary}
                value={searchQuery}
                onChangeText={setSearchQuery}
            />
        </View>
    );

    return (
        <View style={styles.container}>
            {renderHeader()}
            {loading && !refreshing && !stocks ? (
                <View style={styles.center}>
                    <ActivityIndicator size="large" color={colors.primary} />
                </View>
            ) : (
                <FlatList
                    data={stocks || []}
                    keyExtractor={(item) => item.id.toString()}
                    renderItem={({ item }) => (
                        <StockRow stock={item} onPress={() => router.push(`/stock/${item.id}`)} />
                    )}
                    refreshing={refreshing}
                    onRefresh={onRefresh}
                    contentContainerStyle={styles.listContent}
                />
            )}
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
    },
    header: {
        padding: 16,
        borderBottomWidth: 1,
        borderBottomColor: colors.border,
    },
    searchInput: {
        backgroundColor: colors.surfaceHighlight,
        color: colors.text,
        borderRadius: 8,
        padding: 12,
        fontSize: 16,
    },
    listContent: {
        padding: 16,
    }
});
