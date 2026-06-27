import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { colors } from '../theme/colors';

export const SectorBar = ({ sector, changePercent }) => {
    const isPositive = changePercent >= 0;
    const barWidth = Math.min(Math.abs(changePercent) * 10, 100) + '%';
    
    return (
        <View style={styles.container}>
            <View style={styles.header}>
                <Text style={styles.sector}>{sector}</Text>
                <Text style={[styles.change, { color: isPositive ? colors.success : colors.danger }]}>
                    {isPositive ? '+' : ''}{changePercent?.toFixed(2)}%
                </Text>
            </View>
            <View style={styles.track}>
                <View 
                    style={[
                        styles.fill, 
                        { 
                            width: barWidth,
                            backgroundColor: isPositive ? colors.success : colors.danger 
                        }
                    ]} 
                />
            </View>
        </View>
    );
};

const styles = StyleSheet.create({
    container: {
        marginBottom: 16,
    },
    header: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        marginBottom: 8,
    },
    sector: {
        color: colors.text,
        fontSize: 14,
        fontWeight: '500',
    },
    change: {
        fontSize: 14,
        fontWeight: 'bold',
    },
    track: {
        height: 6,
        backgroundColor: colors.surfaceHighlight,
        borderRadius: 3,
        overflow: 'hidden',
    },
    fill: {
        height: '100%',
        borderRadius: 3,
    }
});
