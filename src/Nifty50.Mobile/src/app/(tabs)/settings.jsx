import React from 'react';
import { View, Text, StyleSheet, TouchableOpacity, ScrollView } from 'react-native';
import { useRouter } from 'expo-router';
import { colors } from '../../theme/colors';

export default function SettingsScreen() {
    const router = useRouter();

    return (
        <ScrollView style={styles.container} contentContainerStyle={styles.content}>
            <View style={styles.section}>
                <Text style={styles.sectionTitle}>Preferences</Text>
                <TouchableOpacity style={styles.menuItem}>
                    <Text style={styles.menuText}>Push Notifications</Text>
                    <Text style={styles.menuVal}>Enabled</Text>
                </TouchableOpacity>
                <TouchableOpacity style={styles.menuItem}>
                    <Text style={styles.menuText}>Theme</Text>
                    <Text style={styles.menuVal}>Dark</Text>
                </TouchableOpacity>
            </View>

            <View style={styles.section}>
                <Text style={styles.sectionTitle}>Advanced</Text>
                <TouchableOpacity style={styles.menuItem} onPress={() => router.push('/admin')}>
                    <Text style={styles.menuText}>Admin & Scoring Profiles</Text>
                    <Text style={styles.menuVal}>›</Text>
                </TouchableOpacity>
            </View>

            <View style={styles.footer}>
                <Text style={styles.footerText}>Nifty50 Stock Finder v1.0.0</Text>
            </View>
        </ScrollView>
    );
}

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: colors.background,
    },
    content: {
        padding: 16,
    },
    section: {
        marginBottom: 32,
    },
    sectionTitle: {
        color: colors.textSecondary,
        fontSize: 14,
        fontWeight: 'bold',
        textTransform: 'uppercase',
        marginBottom: 12,
        marginLeft: 8,
    },
    menuItem: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'center',
        backgroundColor: colors.card,
        padding: 16,
        borderBottomWidth: 1,
        borderBottomColor: colors.border,
    },
    menuText: {
        color: colors.text,
        fontSize: 16,
    },
    menuVal: {
        color: colors.textSecondary,
        fontSize: 16,
    },
    footer: {
        alignItems: 'center',
        marginTop: 40,
    },
    footerText: {
        color: colors.textMuted,
        fontSize: 12,
    }
});
