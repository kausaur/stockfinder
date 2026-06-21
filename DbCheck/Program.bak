using System;
using Npgsql;
using System.Collections.Generic;

class Program
{
    static async System.Threading.Tasks.Task Main()
    {
        string connStr = "Host=localhost;Database=stockfinder;Username=postgres;Password=postgres";
        await using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync();

        if (Environment.GetCommandLineArgs().Length > 1 && Environment.GetCommandLineArgs()[1] == "clear")
        {
            Console.WriteLine("Clearing database...");
            await using var cmd = new NpgsqlCommand("TRUNCATE TABLE \"Stocks\" CASCADE", conn);
            await cmd.ExecuteNonQueryAsync();
            Console.WriteLine("Database cleared.");
            return;
        }

        var tables = new[] { "Stocks", "StockPrices", "Dividends", "FinancialStatements", "FundamentalMetrics", "TechnicalIndicators", "SentimentAnalyses", "StockAnalyses" };

        foreach (var table in tables)
        {
            Console.WriteLine($"\n--- Table: {table} ---");
            await using var countCmd = new NpgsqlCommand($"SELECT COUNT(*) FROM \"{table}\"", conn);
            long rowCount = (long)await countCmd.ExecuteScalarAsync();
            Console.WriteLine($"Total Rows: {rowCount}");
            
            if (rowCount == 0) continue;

            await using var colCmd = new NpgsqlCommand($"SELECT column_name FROM information_schema.columns WHERE table_name = '{table}'", conn);
            await using var colReader = await colCmd.ExecuteReaderAsync();
            var columns = new List<string>();
            while (await colReader.ReadAsync())
            {
                columns.Add(colReader.GetString(0));
            }
            await colReader.CloseAsync();

            Console.WriteLine("Checking for NULL values in columns...");
            foreach (var col in columns)
            {
                if (col.EndsWith("Id") || col == "CreatedAt" || col == "UpdatedAt" || col == "IsDeleted" || col == "DeletedAt") continue;
                
                await using var nullCmd = new NpgsqlCommand($"SELECT COUNT(*) FROM \"{table}\" WHERE \"{col}\" IS NULL", conn);
                long nullCount = (long)await nullCmd.ExecuteScalarAsync();
                if (nullCount > 0)
                {
                    Console.WriteLine($"  WARNING: '{col}' has {nullCount} NULL values ({(nullCount * 100.0 / rowCount):F1}%)");
                }
            }
        }
    }
}
