import React from 'react';
import { View, Text, StyleSheet, TouchableOpacity } from 'react-native';
import { colors } from '../theme/colors';

export const WeightStepper = ({ label, value, onIncrement, onDecrement }) => {
    return (
        <View style={styles.container}>
            <Text style={styles.label}>{label}</Text>
            <View style={styles.controls}>
                <TouchableOpacity style={styles.button} onPress={onDecrement}>
                    <Text style={styles.buttonText}>-</Text>
                </TouchableOpacity>
                <Text style={styles.value}>{value}%</Text>
                <TouchableOpacity style={styles.button} onPress={onIncrement}>
                    <Text style={styles.buttonText}>+</Text>
                </TouchableOpacity>
            </View>
        </View>
    );
};

const styles = StyleSheet.create({
    container: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'center',
        paddingVertical: 12,
        borderBottomWidth: 1,
        borderBottomColor: colors.surfaceHighlight,
    },
    label: {
        color: colors.text,
        fontSize: 16,
    },
    controls: {
        flexDirection: 'row',
        alignItems: 'center',
        backgroundColor: colors.surfaceHighlight,
        borderRadius: 8,
    },
    button: {
        paddingHorizontal: 16,
        paddingVertical: 8,
    },
    buttonText: {
        color: colors.text,
        fontSize: 18,
        fontWeight: 'bold',
    },
    value: {
        color: colors.text,
        fontSize: 16,
        fontWeight: 'bold',
        minWidth: 40,
        textAlign: 'center',
    }
});
