import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { colors } from '../theme/colors';

export const SignalBadge = ({ signal }) => {
    const getBadgeStyle = () => {
        switch (signal) {
            case 'Strong Buy':
            case 'Buy':
                return { bg: colors.successBackground, text: colors.success };
            case 'Strong Sell':
            case 'Sell':
                return { bg: colors.dangerBackground, text: colors.danger };
            default:
                return { bg: colors.warningBackground, text: colors.warning };
        }
    };

    const style = getBadgeStyle();

    return (
        <View style={[styles.badge, { backgroundColor: style.bg }]}>
            <Text style={[styles.text, { color: style.text }]}>{signal}</Text>
        </View>
    );
};

const styles = StyleSheet.create({
    badge: {
        paddingHorizontal: 8,
        paddingVertical: 4,
        borderRadius: 4,
        alignSelf: 'flex-start',
    },
    text: {
        fontSize: 12,
        fontWeight: 'bold',
    }
});
