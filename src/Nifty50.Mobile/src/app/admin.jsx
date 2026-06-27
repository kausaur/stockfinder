import React, { useState } from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity, ActivityIndicator, Alert } from 'react-native';
import { api } from '../services/api';
import { useApi } from '../hooks/useApi';
import { colors } from '../theme/colors';
import { PresetCard } from '../components/PresetCard';
import { WeightStepper } from '../components/WeightStepper';

export default function AdminScreen() {
    const { data: profiles, loading: loadingProfiles, refetch: refetchProfiles } = useApi(api.getScoringProfiles, 'scoring_profiles');
    const { data: activeProfile, refetch: refetchActive } = useApi(api.getActiveProfile, 'active_profile');
    const [refreshing, setRefreshing] = useState(false);

    const handleActivateProfile = async (id) => {
        try {
            await api.activateProfile(id);
            Alert.alert('Success', 'Profile activated successfully');
            await refetchActive();
            await refetchProfiles();
        } catch (error) {
            Alert.alert('Error', 'Failed to activate profile');
        }
    };

    const handleTriggerRefresh = async () => {
        try {
            setRefreshing(true);
            await api.triggerRefresh();
            Alert.alert('Success', 'Data refresh triggered. This may take a few minutes.');
        } catch (error) {
            Alert.alert('Error', 'Failed to trigger data refresh');
        } finally {
            setRefreshing(false);
        }
    };

    if (loadingProfiles) {
        return (
            <View style={styles.center}>
                <ActivityIndicator size="large" color={colors.primary} />
            </View>
        );
    }

    return (
        <ScrollView style={styles.container}>
            <View style={styles.section}>
                <Text style={styles.sectionTitle}>Data Management</Text>
                <TouchableOpacity 
                    style={styles.actionButton} 
                    onPress={handleTriggerRefresh}
                    disabled={refreshing}
                >
                    {refreshing ? (
                        <ActivityIndicator color="#fff" />
                    ) : (
                        <Text style={styles.actionButtonText}>Trigger Manual Data Refresh</Text>
                    )}
                </TouchableOpacity>
                <Text style={styles.helperText}>Force the system to fetch the latest stock prices and run the analysis engine immediately.</Text>
            </View>

            <View style={styles.section}>
                <Text style={styles.sectionTitle}>Scoring Profiles</Text>
                <Text style={styles.helperText}>Select the algorithm used to generate buy/sell signals.</Text>
                
                {profiles?.map(profile => (
                    <PresetCard 
                        key={profile.id} 
                        profile={profile} 
                        isActive={activeProfile?.id === profile.id}
                        onPress={() => handleActivateProfile(profile.id)}
                    />
                ))}
            </View>
        </ScrollView>
    );
}

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: colors.background,
        padding: 16,
    },
    center: {
        flex: 1,
        justifyContent: 'center',
        alignItems: 'center',
    },
    section: {
        marginBottom: 32,
    },
    sectionTitle: {
        color: colors.text,
        fontSize: 20,
        fontWeight: 'bold',
        marginBottom: 8,
    },
    helperText: {
        color: colors.textMuted,
        fontSize: 14,
        marginBottom: 16,
    },
    actionButton: {
        backgroundColor: colors.primary,
        padding: 16,
        borderRadius: 8,
        alignItems: 'center',
        marginBottom: 8,
    },
    actionButtonText: {
        color: '#fff',
        fontSize: 16,
        fontWeight: 'bold',
    }
});
