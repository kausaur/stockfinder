import React from 'react';
import { View, Text, StyleSheet, TouchableOpacity } from 'react-native';
import { colors } from '../theme/colors';
import { SignalBadge } from './SignalBadge';

export const StockRow = ({ stock, onPress }) => {
    const isPositive = stock.dayChangePercent >= 0;
    
    return (
        <TouchableOpacity style={styles.container} onPress={onPress} activeOpacity={0.7}>
            <View style={styles.left}>
                <Text style={styles.symbol}>{stock.symbol}</Text>
                <Text style={styles.companyName} numberOfLines={1}>{stock.companyName}</Text>
            </View>
            <View style={styles.middle}>
                {stock.overallSignal && <SignalBadge signal={stock.overallSignal} />}
            </View>
            <View style={styles.right}>
                <Text style={styles.price}>₹{stock.currentPrice?.toFixed(2)}</Text>
                <Text style={[styles.change, { color: isPositive ? colors.success : colors.danger }]}>
                    {isPositive ? '+' : ''}{stock.dayChangePercent?.toFixed(2)}%
                </Text>
            </View>
        </TouchableOpacity>
    );
};

const styles = StyleSheet.create({
    container: {
        flexDirection: 'row',
        padding: 16,
        backgroundColor: colors.card,
        borderRadius: 12,
        marginBottom: 8,
        alignItems: 'center',
        justifyContent: 'space-between',
        borderWidth: 1,
        borderColor: colors.border,
    },
    left: {
        flex: 2,
    },
    symbol: {
        color: colors.text,
        fontSize: 16,
        fontWeight: 'bold',
        marginBottom: 4,
    },
    companyName: {
        color: colors.textSecondary,
        fontSize: 12,
    },
    middle: {
        flex: 1.5,
        alignItems: 'center',
    },
    right: {
        flex: 1.5,
        alignItems: 'flex-end',
    },
    price: {
        color: colors.text,
        fontSize: 14,
        fontWeight: 'bold',
        marginBottom: 4,
    },
    change: {
        fontSize: 12,
        fontWeight: '600',
    }
});
