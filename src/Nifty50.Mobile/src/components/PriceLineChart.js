import React, { useState, useMemo } from 'react';
import { View, Text, StyleSheet, Dimensions, TouchableOpacity } from 'react-native';
import { LineChart } from 'react-native-gifted-charts';
import { colors } from '../theme/colors';

const { width } = Dimensions.get('window');

const DURATIONS = ['1M', '3M', '6M', '1Y', '5Y'];

export const PriceLineChart = ({ prices, isPositive = true }) => {
    const [duration, setDuration] = useState('3M');

    const chartData = useMemo(() => {
        if (!prices || prices.length < 2) return [];

        let days = 0;
        switch (duration) {
            case '1M': days = 22; break; // approx 22 trading days
            case '3M': days = 65; break;
            case '6M': days = 130; break;
            case '1Y': days = 260; break;
            case '5Y': days = 1300; break;
            default: days = prices.length;
        }

        // Slice to the requested duration
        const sliced = prices.length > days ? prices.slice(-days) : prices;

        // Downsample to max ~250 points for high fidelity without crashing
        const maxPoints = 250;
        let downsampled = sliced;
        if (sliced.length > maxPoints) {
            const step = Math.ceil(sliced.length / maxPoints);
            downsampled = sliced.filter((_, i) => i % step === 0);
        }

        return downsampled.map(p => ({
            value: p.close || 0,
            date: new Date(p.date).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
        }));
    }, [prices, duration]);

    if (chartData.length < 2) {
        return (
            <View style={styles.emptyContainer}>
                <Text style={styles.emptyText}>Not enough price data for chart</Text>
            </View>
        );
    }

    const lineColor = isPositive ? colors.success : colors.danger;
    
    // Calculate min/max for better y-axis scaling safely
    const minVal = chartData.reduce((min, d) => d.value < min ? d.value : min, chartData[0].value);
    const maxVal = chartData.reduce((max, d) => d.value > max ? d.value : max, chartData[0].value);
    const padding = (maxVal - minVal) * 0.1;

    const chartWidth = width - 40;
    const dynamicSpacing = chartWidth / Math.max(1, chartData.length - 1);

    return (
        <View style={styles.container}>
            <View style={styles.filterRow}>
                {DURATIONS.map(d => (
                    <TouchableOpacity 
                        key={d} 
                        style={[styles.filterBtn, duration === d && styles.filterBtnActive]}
                        onPress={() => setDuration(d)}
                    >
                        <Text style={[styles.filterText, duration === d && styles.filterTextActive]}>{d}</Text>
                    </TouchableOpacity>
                ))}
            </View>

            <LineChart
                key={`${duration}-${chartData.length}`} // force re-render on duration change
                data={chartData}
                width={chartWidth}
                height={200}
                thickness={2}
                color={lineColor}
                spacing={dynamicSpacing}
                hideDataPoints
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
                pointerConfig={{
                    pointerStripHeight: 200,
                    pointerStripColor: colors.border,
                    pointerStripWidth: 2,
                    pointerColor: lineColor,
                    radius: 4,
                    pointerLabelWidth: 90,
                    pointerLabelHeight: 50,
                    autoAdjustPointerLabelPosition: true,
                    pointerLabelComponent: items => {
                        if (!items || !items[0]) return null;
                        return (
                            <View style={styles.tooltip}>
                                <Text style={styles.tooltipDate}>{items[0].date}</Text>
                                <Text style={styles.tooltipPrice}>₹{items[0].value.toFixed(2)}</Text>
                            </View>
                        );
                    }
                }}
            />
        </View>
    );
};

const styles = StyleSheet.create({
    container: {
        marginVertical: 16,
        alignItems: 'center',
    },
    filterRow: {
        flexDirection: 'row',
        justifyContent: 'center',
        marginBottom: 16,
        backgroundColor: colors.surface,
        borderRadius: 8,
        padding: 4,
    },
    filterBtn: {
        paddingVertical: 6,
        paddingHorizontal: 16,
        borderRadius: 6,
    },
    filterBtnActive: {
        backgroundColor: colors.primary,
    },
    filterText: {
        color: colors.textSecondary,
        fontSize: 12,
        fontWeight: '600',
    },
    filterTextActive: {
        color: colors.text,
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
    },
    tooltip: {
        backgroundColor: colors.card,
        padding: 8,
        borderRadius: 8,
        borderWidth: 1,
        borderColor: colors.border,
        alignItems: 'center',
        justifyContent: 'center',
        shadowColor: '#000',
        shadowOffset: { width: 0, height: 2 },
        shadowOpacity: 0.3,
        shadowRadius: 4,
        elevation: 5,
    },
    tooltipDate: {
        color: colors.textSecondary,
        fontSize: 10,
        marginBottom: 2,
    },
    tooltipPrice: {
        color: colors.text,
        fontSize: 12,
        fontWeight: 'bold',
    }
});
