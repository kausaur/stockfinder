import { useState, useEffect } from 'react';
import { View, Text, ScrollView, TouchableOpacity, ActivityIndicator, TextInput, Modal } from 'react-native';
import { useRouter } from 'expo-router';
import { api } from '../../services/api';
import { SafeAreaView } from 'react-native-safe-area-context';

export default function ScreenerScreen() {
    const [results, setResults] = useState([]);
    const [loading, setLoading] = useState(false);
    const [filters, setFilters] = useState({
        MinScore: '60',
        Signals: 'Buy',
    });
    const router = useRouter();

    const fetchScreenerResults = async () => {
        setLoading(true);
        try {
            const cleanFilters = {};
            if (filters.MinScore) cleanFilters.MinScore = parseInt(filters.MinScore);
            if (filters.Signals) cleanFilters.Signals = filters.Signals.split(',').map(s => s.trim());
            
            const res = await api.getScreenerResults(cleanFilters);
            setResults(res.data);
        } catch (error) {
            console.error('Failed to fetch screener results:', error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchScreenerResults();
    }, []);

    const updateFilter = (key, value) => {
        setFilters(prev => ({ ...prev, [key]: value }));
    };

    const renderStockCard = (stock) => (
        <TouchableOpacity
            key={stock.stockId}
            className="bg-slate-900 border border-slate-800 rounded-xl p-4 mb-3"
            onPress={() => router.push(`/stock/${stock.stockId}`)}
        >
            <View className="flex-row justify-between items-center">
                <View>
                    <Text className="text-white font-bold text-base">{stock.symbol}</Text>
                    <Text className="text-slate-400 text-xs">{stock.sector}</Text>
                </View>
                <View className="items-end">
                    <Text className="text-white font-mono font-bold">₹{stock.currentPrice?.toFixed(2)}</Text>
                    <View className="bg-blue-500/10 px-2 py-0.5 rounded mt-1">
                        <Text className="text-blue-400 text-xs font-bold">Score: {stock.overallScore}</Text>
                    </View>
                </View>
            </View>
        </TouchableOpacity>
    );

    return (
        <SafeAreaView className="flex-1 bg-slate-950" edges={['top']}>
            <View className="px-5 py-4 border-b border-slate-800 flex-row justify-between items-center">
                <Text className="text-2xl font-bold text-white">Screener</Text>
            </View>

            <View className="p-4 border-b border-slate-800 bg-slate-900/50">
                <View className="flex-row gap-2 mb-3">
                    <View className="flex-1">
                        <Text className="text-xs text-slate-400 mb-1">Min Overall Score</Text>
                        <TextInput 
                            className="bg-slate-800 text-white p-2 rounded-lg text-sm"
                            keyboardType="numeric"
                            value={filters.MinScore}
                            onChangeText={v => updateFilter('MinScore', v)}
                            placeholder="e.g. 60"
                            placeholderTextColor="#64748b"
                        />
                    </View>
                    <View className="flex-1">
                        <Text className="text-xs text-slate-400 mb-1">Signals</Text>
                        <TextInput 
                            className="bg-slate-800 text-white p-2 rounded-lg text-sm"
                            value={filters.Signals}
                            onChangeText={v => updateFilter('Signals', v)}
                            placeholder="Buy, Strong Buy"
                            placeholderTextColor="#64748b"
                        />
                    </View>
                </View>
                
                <TouchableOpacity 
                    className="bg-blue-600 py-3 rounded-xl items-center"
                    onPress={fetchScreenerResults}
                    disabled={loading}
                >
                    {loading ? (
                        <ActivityIndicator size="small" color="#ffffff" />
                    ) : (
                        <Text className="text-white font-bold text-sm">Run Screener</Text>
                    )}
                </TouchableOpacity>
            </View>

            <ScrollView className="flex-1 px-5 pt-4">
                <Text className="text-sm text-slate-400 mb-4">
                    {results.length} {results.length === 1 ? 'result' : 'results'} found
                </Text>
                
                {results.map(renderStockCard)}
                
                <View className="h-20" />
            </ScrollView>
        </SafeAreaView>
    );
}
