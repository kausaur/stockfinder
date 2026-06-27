import React from 'react';
import { View, Text, StyleSheet, ScrollView, ActivityIndicator } from 'react-native';
import { useLocalSearchParams } from 'expo-router';
import { api } from '../../services/api';
import { useApi } from '../../hooks/useApi';
import { colors } from '../../theme/colors';
import { ScoreGauge } from '../../components/ScoreGauge';
import { SignalBadge } from '../../components/SignalBadge';
import { CollapsibleCard } from '../../components/CollapsibleCard';
import { PriceLineChart } from '../../components/PriceLineChart';

export default function StockDetailScreen() {
    const { id } = useLocalSearchParams();
    const { data: stock, loading: loadingStock } = useApi(() => api.getStock(id), `stock_${id}`, [id]);
    const { data: analysis, loading: loadingAnalysis } = useApi(() => api.getAnalysis(id), `analysis_${id}`, [id]);
    const { data: prices, loading: loadingPrices } = useApi(() => api.getStockPrices(id), `prices_${id}`, [id]);
    const { data: tech } = useApi(() => api.getTechnical(id), `tech_${id}`, [id]);
    const { data: fund } = useApi(() => api.getFundamental(id), `fund_${id}`, [id]);
    const { data: sentiment } = useApi(() => api.getSentiment(id), `sentiment_${id}`, [id]);

    const loading = loadingStock || loadingAnalysis;

    if (loading && !stock) {
        return (
            <View style={styles.center}>
                <ActivityIndicator size="large" color={colors.primary} />
            </View>
        );
    }

    if (!stock) {
        return (
            <View style={styles.center}>
                <Text style={styles.errorText}>Stock not found</Text>
            </View>
        );
    }

    const isPositive = stock.dayChangePercent >= 0;

    return (
        <ScrollView style={styles.container}>
            <View style={styles.header}>
                <View>
                    <Text style={styles.symbol}>{stock.symbol}</Text>
                    <Text style={styles.companyName}>{stock.companyName}</Text>
                </View>
                <View style={styles.priceContainer}>
                    <Text style={styles.price}>₹{stock.currentPrice?.toFixed(2)}</Text>
                    <Text style={[styles.change, { color: isPositive ? colors.success : colors.danger }]}>
                        {isPositive ? '+' : ''}{stock.dayChangePercent?.toFixed(2)}%
                    </Text>
                </View>
            </View>

            {analysis && (
                <View style={styles.analysisHeader}>
                    <ScoreGauge score={analysis.overallScore} size={120} label="Overall Score" />
                    <View style={styles.signalContainer}>
                        <Text style={styles.signalLabel}>Signal</Text>
                        <SignalBadge signal={analysis.overallSignal} />
                        <Text style={styles.reasoning} numberOfLines={3}>{analysis.reasoning}</Text>
                    </View>
                </View>
            )}

            {!loadingPrices && prices && (
                <PriceLineChart prices={prices} isPositive={isPositive} />
            )}

            <CollapsibleCard title="Technical Analysis" defaultExpanded={true}>
                {analysis && (
                    <View style={styles.scoreRow}>
                        <Text style={styles.scoreText}>Technical Score: {analysis.technicalScore}</Text>
                        <SignalBadge signal={analysis.technicalSignal} />
                    </View>
                )}
                {tech && (
                    <View style={styles.dataGrid}>
                        <View style={styles.dataCol}><Text style={styles.dataLabel}>RSI</Text><Text style={styles.dataVal}>{tech.rsI14?.toFixed(2) || 'N/A'}</Text></View>
                        <View style={styles.dataCol}><Text style={styles.dataLabel}>MACD</Text><Text style={styles.dataVal}>{tech.macd?.toFixed(2) || 'N/A'}</Text></View>
                        <View style={styles.dataCol}><Text style={styles.dataLabel}>SMA50</Text><Text style={styles.dataVal}>{tech.smA50?.toFixed(2) || 'N/A'}</Text></View>
                    </View>
                )}
            </CollapsibleCard>

            <CollapsibleCard title="Fundamental Analysis">
                {analysis && (
                    <View style={styles.scoreRow}>
                        <Text style={styles.scoreText}>Fundamental Score: {analysis.fundamentalScore}</Text>
                        <SignalBadge signal={analysis.fundamentalSignal} />
                    </View>
                )}
                {fund && (
                    <View style={styles.dataGrid}>
                        <View style={styles.dataCol}><Text style={styles.dataLabel}>P/E</Text><Text style={styles.dataVal}>{fund.peRatio?.toFixed(2) || 'N/A'}</Text></View>
                        <View style={styles.dataCol}><Text style={styles.dataLabel}>ROE</Text><Text style={styles.dataVal}>{fund.roe?.toFixed(2) || 'N/A'}%</Text></View>
                        <View style={styles.dataCol}><Text style={styles.dataLabel}>D/E</Text><Text style={styles.dataVal}>{fund.debtToEquity?.toFixed(2) || 'N/A'}</Text></View>
                    </View>
                )}
            </CollapsibleCard>

            <CollapsibleCard title="Sentiment Analysis">
                {analysis && (
                    <View style={styles.scoreRow}>
                        <Text style={styles.scoreText}>Sentiment Score: {analysis.sentimentScore}</Text>
                        <SignalBadge signal={analysis.sentimentSignal} />
                    </View>
                )}
                {sentiment && (
                    <View style={styles.dataGrid}>
                        <View style={styles.dataCol}><Text style={styles.dataLabel}>Positive</Text><Text style={[styles.dataVal, {color: colors.success}]}>{sentiment.positiveCount || 0}</Text></View>
                        <View style={styles.dataCol}><Text style={styles.dataLabel}>Negative</Text><Text style={[styles.dataVal, {color: colors.danger}]}>{sentiment.negativeCount || 0}</Text></View>
                        <View style={styles.dataCol}><Text style={styles.dataLabel}>Neutral</Text><Text style={styles.dataVal}>{sentiment.neutralCount || 0}</Text></View>
                    </View>
                )}
            </CollapsibleCard>

            <View style={{ height: 40 }} />
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
        backgroundColor: colors.background,
        justifyContent: 'center',
        alignItems: 'center',
    },
    errorText: {
        color: colors.danger,
        fontSize: 16,
    },
    header: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        marginBottom: 24,
    },
    symbol: {
        color: colors.text,
        fontSize: 24,
        fontWeight: 'bold',
    },
    companyName: {
        color: colors.textSecondary,
        fontSize: 14,
    },
    priceContainer: {
        alignItems: 'flex-end',
    },
    price: {
        color: colors.text,
        fontSize: 24,
        fontWeight: 'bold',
    },
    change: {
        fontSize: 16,
        fontWeight: 'bold',
    },
    analysisHeader: {
        flexDirection: 'row',
        backgroundColor: colors.card,
        borderRadius: 12,
        padding: 16,
        marginBottom: 16,
        borderWidth: 1,
        borderColor: colors.border,
        alignItems: 'center',
    },
    signalContainer: {
        flex: 1,
        marginLeft: 16,
        justifyContent: 'center',
    },
    signalLabel: {
        color: colors.textSecondary,
        fontSize: 12,
        marginBottom: 4,
    },
    reasoning: {
        color: colors.textMuted,
        fontSize: 12,
        marginTop: 8,
        lineHeight: 16,
    },
    scoreRow: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        alignItems: 'center',
        marginBottom: 16,
    },
    scoreText: {
        color: colors.text,
        fontWeight: '600',
    },
    dataGrid: {
        flexDirection: 'row',
        justifyContent: 'space-between',
    },
    dataCol: {
        alignItems: 'center',
    },
    dataLabel: {
        color: colors.textSecondary,
        fontSize: 12,
        marginBottom: 4,
    },
    dataVal: {
        color: colors.text,
        fontWeight: 'bold',
    }
});
