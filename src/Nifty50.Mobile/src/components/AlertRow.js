import React from 'react';
import { View, Text, StyleSheet, TouchableOpacity } from 'react-native';
import { colors } from '../theme/colors';
import { SignalBadge } from './SignalBadge';

export const AlertRow = ({ alert, onPress }) => {
    return (
        <TouchableOpacity style={styles.container} onPress={onPress} activeOpacity={0.7}>
            <View style={styles.header}>
                <View style={styles.titleRow}>
                    <Text style={styles.symbol}>{alert.symbol}</Text>
                    <SignalBadge signal={alert.overallSignal} />
                </View>
                <Text style={styles.date}>{new Date(alert.analyzedAt).toLocaleDateString()}</Text>
            </View>
            <View style={styles.content}>
                <Text style={styles.message}>{alert.alertMessage}</Text>
                <Text style={styles.reasoning} numberOfLines={2}>{alert.reasoning}</Text>
            </View>
            <View style={styles.footer}>
                <View style={styles.scorePill}>
                    <Text style={styles.scoreLabel}>Tech</Text>
                    <Text style={styles.scoreVal}>{alert.technicalScore}</Text>
                </View>
                <View style={styles.scorePill}>
                    <Text style={styles.scoreLabel}>Fund</Text>
                    <Text style={styles.scoreVal}>{alert.fundamentalScore}</Text>
                </View>
                <View style={styles.scorePill}>
                    <Text style={styles.scoreLabel}>Sent</Text>
                    <Text style={styles.scoreVal}>{alert.sentimentScore}</Text>
                </View>
                <View style={styles.scorePill}>
                    <Text style={styles.scoreLabel}>Div</Text>
                    <Text style={styles.scoreVal}>{alert.dividendScore}</Text>
                </View>
            </View>
        </TouchableOpacity>
    );
};

const styles = StyleSheet.create({
    container: {
        backgroundColor: colors.card,
        borderRadius: 12,
        padding: 16,
        marginBottom: 12,
        borderWidth: 1,
        borderColor: colors.border,
    },
    header: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginBottom: 12,
    },
    titleRow: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: 12,
    },
    symbol: {
        color: colors.text,
        fontSize: 18,
        fontWeight: 'bold',
    },
    date: {
        color: colors.textSecondary,
        fontSize: 12,
    },
    content: {
        marginBottom: 16,
    },
    message: {
        color: colors.text,
        fontSize: 14,
        fontWeight: '600',
        marginBottom: 8,
    },
    reasoning: {
        color: colors.textMuted,
        fontSize: 13,
        lineHeight: 18,
    },
    footer: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        borderTopWidth: 1,
        borderTopColor: colors.surfaceHighlight,
        paddingTop: 12,
    },
    scorePill: {
        alignItems: 'center',
    },
    scoreLabel: {
        color: colors.textSecondary,
        fontSize: 10,
        marginBottom: 4,
    },
    scoreVal: {
        color: colors.text,
        fontSize: 14,
        fontWeight: 'bold',
    }
});
