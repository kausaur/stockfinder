import { useState, useEffect } from 'react';
import { View, Text, ScrollView, TouchableOpacity, ActivityIndicator, RefreshControl } from 'react-native';
import { useRouter } from 'expo-router';
import { api } from '../../services/api';
import { SafeAreaView } from 'react-native-safe-area-context';

export default function RecommendationsScreen() {
    const [picks, setPicks] = useState(null);
    const [loading, setLoading] = useState(true);
    const [refreshing, setRefreshing] = useState(false);
    const router = useRouter();

    const fetchPicks = async () => {
        try {
            const res = await api.getRecommendations();
            setPicks(res.data);
        } catch (error) {
            console.error('Failed to fetch recommendations:', error);
        } finally {
            setLoading(false);
            setRefreshing(false);
        }
    };

    useEffect(() => {
        fetchPicks();
    }, []);

    const onRefresh = () => {
        setRefreshing(true);
        fetchPicks();
    };

    if (loading && !refreshing) {
        return (
            <View className="flex-1 bg-slate-950 items-center justify-center">
                <ActivityIndicator size="large" color="#3b82f6" />
            </View>
        );
    }

    if (!picks) {
        return (
            <View className="flex-1 bg-slate-950 items-center justify-center">
                <Text className="text-slate-400">Failed to load recommendations.</Text>
            </View>
        );
    }

    const renderStockCard = (stock, index) => (
        <TouchableOpacity
            key={index}
            className="bg-slate-900 border border-slate-800 rounded-xl p-4 mb-4"
            onPress={() => router.push(`/stock/${stock.id}`)}
        >
            <View className="flex-row justify-between items-start mb-2">
                <View>
                    <Text className="text-white font-bold text-lg">{stock.symbol}</Text>
                    <Text className="text-slate-400 text-xs">{stock.companyName}</Text>
                </View>
                <View className="items-end">
                    <Text className="text-white font-mono font-bold">₹{stock.currentPrice?.toFixed(2)}</Text>
                    <Text className={`text-xs font-bold ${stock.upsidePercent > 0 ? 'text-emerald-400' : 'text-red-400'}`}>
                        Upside: {stock.upsidePercent?.toFixed(2)}%
                    </Text>
                </View>
            </View>
            
            <View className="flex-row justify-between items-center mt-3 pt-3 border-t border-slate-800">
                <View className="flex-row space-x-2">
                    <View className="bg-blue-500/10 px-2 py-1 rounded">
                        <Text className="text-blue-400 text-xs font-bold">Overall: {stock.overallScore}</Text>
                    </View>
                    <View className="bg-teal-500/10 px-2 py-1 rounded">
                        <Text className="text-teal-400 text-xs font-bold">Quality: {stock.qualityScore}</Text>
                    </View>
                </View>
                <View className={`px-3 py-1 rounded-full ${stock.overallSignal === 'Strong Buy' ? 'bg-emerald-500' : 'bg-blue-500'}`}>
                    <Text className="text-white text-xs font-bold">{stock.overallSignal}</Text>
                </View>
            </View>
        </TouchableOpacity>
    );

    return (
        <SafeAreaView className="flex-1 bg-slate-950" edges={['top']}>
            <View className="px-5 py-4 border-b border-slate-800 flex-row justify-between items-center">
                <Text className="text-2xl font-bold text-white">Top Picks</Text>
            </View>

            <ScrollView 
                className="flex-1 px-5 pt-4"
                refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor="#3b82f6" />}
            >
                <View className="mb-6">
                    <Text className="text-sm font-semibold text-slate-400 mb-3 uppercase tracking-wider">Strong Buys</Text>
                    {picks.strongBuys.length > 0 ? 
                        picks.strongBuys.map(renderStockCard) : 
                        <Text className="text-slate-500 italic mb-4">No strong buy recommendations currently.</Text>
                    }
                </View>

                <View className="mb-6">
                    <Text className="text-sm font-semibold text-slate-400 mb-3 uppercase tracking-wider">Value Picks (High Upside)</Text>
                    {picks.valuePicks.length > 0 ? 
                        picks.valuePicks.map(renderStockCard) : 
                        <Text className="text-slate-500 italic mb-4">No value picks currently.</Text>
                    }
                </View>

                <View className="mb-6">
                    <Text className="text-sm font-semibold text-slate-400 mb-3 uppercase tracking-wider">Quality Performers</Text>
                    {picks.qualityPicks.length > 0 ? 
                        picks.qualityPicks.map(renderStockCard) : 
                        <Text className="text-slate-500 italic mb-4">No quality picks currently.</Text>
                    }
                </View>
                
                <View className="h-20" />
            </ScrollView>
        </SafeAreaView>
    );
}
