import React, { useState } from 'react';
import { View, Text, StyleSheet, TouchableOpacity, LayoutAnimation, UIManager, Platform } from 'react-native';
import { colors } from '../theme/colors';

if (Platform.OS === 'android' && UIManager.setLayoutAnimationEnabledExperimental) {
    UIManager.setLayoutAnimationEnabledExperimental(true);
}

export const CollapsibleCard = ({ title, children, defaultExpanded = false }) => {
    const [expanded, setExpanded] = useState(defaultExpanded);

    const toggle = () => {
        LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
        setExpanded(!expanded);
    };

    return (
        <View style={styles.container}>
            <TouchableOpacity style={styles.header} onPress={toggle} activeOpacity={0.7}>
                <Text style={styles.title}>{title}</Text>
                <Text style={styles.icon}>{expanded ? '▲' : '▼'}</Text>
            </TouchableOpacity>
            {expanded && (
                <View style={styles.content}>
                    {children}
                </View>
            )}
        </View>
    );
};

const styles = StyleSheet.create({
    container: {
        backgroundColor: colors.card,
        borderRadius: 12,
        marginBottom: 16,
        overflow: 'hidden',
        borderWidth: 1,
        borderColor: colors.border,
    },
    header: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        padding: 16,
        backgroundColor: colors.surfaceHighlight,
    },
    title: {
        color: colors.text,
        fontSize: 16,
        fontWeight: 'bold',
    },
    icon: {
        color: colors.textSecondary,
        fontSize: 12,
    },
    content: {
        padding: 16,
    }
});
