import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { PieChart } from 'react-native-gifted-charts';
import { colors } from '../theme/colors';

export const ScoreGauge = ({ score, size = 100, label = 'Overall' }) => {
    
    const getColor = (s) => {
        if (s >= 60) return colors.success;
        if (s >= 40) return colors.warning;
        return colors.danger;
    };

    const color = getColor(score);
    const data = [
        { value: score, color: color },
        { value: 100 - score, color: colors.surfaceHighlight }
    ];

    return (
        <View style={styles.container}>
            <PieChart
                donut
                radius={size / 2}
                innerRadius={(size / 2) - 10}
                data={data}
                centerLabelComponent={() => (
                    <View style={styles.centerLabel}>
                        <Text style={[styles.scoreText, { color }]}>{score}</Text>
                    </View>
                )}
            />
            <Text style={styles.labelText}>{label}</Text>
        </View>
    );
};

const styles = StyleSheet.create({
    container: {
        alignItems: 'center',
        justifyContent: 'center',
    },
    centerLabel: {
        alignItems: 'center',
        justifyContent: 'center',
    },
    scoreText: {
        fontSize: 18,
        fontWeight: 'bold',
    },
    labelText: {
        color: colors.textSecondary,
        fontSize: 12,
        marginTop: 8,
        fontWeight: '500',
    }
});
