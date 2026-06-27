import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { colors } from '../theme/colors';
import { useNetwork } from '../hooks/useNetwork';

export const OfflineBanner = () => {
    const isOnline = useNetwork();

    if (isOnline) return null;

    return (
        <View style={styles.container}>
            <Text style={styles.text}>You are offline. Showing cached data.</Text>
        </View>
    );
};

const styles = StyleSheet.create({
    container: {
        backgroundColor: colors.danger,
        padding: 8,
        alignItems: 'center',
        justifyContent: 'center',
    },
    text: {
        color: '#FFFFFF',
        fontSize: 12,
        fontWeight: '600',
    }
});
