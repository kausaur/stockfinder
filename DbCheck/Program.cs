using System;
using Npgsql;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var connStr = "Host=localhost;Database=stockfinder;Username=postgres;Password=postgres";
        await using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync();

        // Clear analyses first (FK dependency), then scoring profiles
        await using var cmd1 = new NpgsqlCommand(@"DELETE FROM ""StockAnalyses""", conn);
        var a = await cmd1.ExecuteNonQueryAsync();
        Console.WriteLine($"Deleted {a} stock analyses.");

        await using var cmd2 = new NpgsqlCommand(@"DELETE FROM ""ScoringProfiles""", conn);
        var p = await cmd2.ExecuteNonQueryAsync();
        Console.WriteLine($"Deleted {p} scoring profiles. App will re-seed on next start.");
    }
}
