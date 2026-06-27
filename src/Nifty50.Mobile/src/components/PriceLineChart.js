import React from 'react';
import { View, Text, StyleSheet, Dimensions } from 'react-native';
import { LineChart } from 'react-native-gifted-charts';
import { colors } from '../theme/colors';

const { width } = Dimensions.get('window');

export const PriceLineChart = ({ prices, isPositive = true }) => {
    if (!prices || prices.length === 0) {
        return (
            <View style={styles.emptyContainer}>
                <Text style={styles.emptyText}>No price data available</Text>
            </View>
        );
    }

    const data = prices.map(p => ({
        value: p.close,
        date: new Date(p.date).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
    }));

    const lineColor = isPositive ? colors.success : colors.danger;
    
    // Calculate min/max for better y-axis scaling
    const minVal = Math.min(...data.map(d => d.value));
    const maxVal = Math.max(...data.map(d => d.value));
    const padding = (maxVal - minVal) * 0.1;

    return (
        <View style={styles.container}>
            <LineChart
                data={data}
                width={width - 40}
                height={200}
                thickness={2}
                color={lineColor}
                hideRules
                hideYAxisText
                hideAxesAndRules
                curved
                initialSpacing={0}
                endSpacing={0}
                maxValue={maxVal + padding}
                minValue={Math.max(0, minVal - padding)}
                areaChart
                startFillColor={lineColor}
                endFillColor={colors.background}
                startOpacity={0.4}
                endOpacity={0.0}
            />
        </View>
    );
};

const styles = StyleSheet.create({
    container: {
        marginVertical: 16,
        alignItems: 'center',
    },
    emptyContainer: {
        height: 200,
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: colors.surface,
        borderRadius: 12,
        marginVertical: 16,
    },
    emptyText: {
        color: colors.textSecondary,
    }
});
