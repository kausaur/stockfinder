import React from 'react';
import { View, Text, StyleSheet, TouchableOpacity } from 'react-native';
import { colors } from '../theme/colors';

export const PresetCard = ({ profile, isActive, onPress }) => {
    return (
        <TouchableOpacity 
            style={[styles.container, isActive && styles.activeContainer]} 
            onPress={onPress}
            activeOpacity={0.7}
        >
            <View style={styles.header}>
                <Text style={styles.name}>{profile.name}</Text>
                {isActive && (
                    <View style={styles.badge}>
                        <Text style={styles.badgeText}>Active</Text>
                    </View>
                )}
            </View>
            
            <View style={styles.weights}>
                <View style={styles.weightCol}>
                    <Text style={styles.label}>Tech</Text>
                    <Text style={styles.val}>{profile.technicalWeight}%</Text>
                </View>
                <View style={styles.weightCol}>
                    <Text style={styles.label}>Fund</Text>
                    <Text style={styles.val}>{profile.fundamentalWeight}%</Text>
                </View>
                <View style={styles.weightCol}>
                    <Text style={styles.label}>Sent</Text>
                    <Text style={styles.val}>{profile.sentimentWeight}%</Text>
                </View>
                <View style={styles.weightCol}>
                    <Text style={styles.label}>Div</Text>
                    <Text style={styles.val}>{profile.dividendWeight}%</Text>
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
    activeContainer: {
        borderColor: colors.primary,
        backgroundColor: 'rgba(59, 130, 246, 0.1)',
    },
    header: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginBottom: 16,
    },
    name: {
        color: colors.text,
        fontSize: 18,
        fontWeight: 'bold',
    },
    badge: {
        backgroundColor: colors.primary,
        paddingHorizontal: 8,
        paddingVertical: 4,
        borderRadius: 12,
    },
    badgeText: {
        color: '#fff',
        fontSize: 10,
        fontWeight: 'bold',
    },
    weights: {
        flexDirection: 'row',
        justifyContent: 'space-between',
    },
    weightCol: {
        alignItems: 'center',
    },
    label: {
        color: colors.textSecondary,
        fontSize: 12,
        marginBottom: 4,
    },
    val: {
        color: colors.text,
        fontSize: 14,
        fontWeight: '600',
    }
});
