using Npgsql;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: DbBatchRunner <connection-string> <path-to.sql>");
    return 1;
}

var connStr = args[0];
var path = args[1];
var sql = await File.ReadAllTextAsync(path);

await using var conn = new NpgsqlConnection(connStr);
await conn.OpenAsync();
await using var cmd = new NpgsqlCommand(sql, conn);
cmd.CommandTimeout = 120;

if (sql.TrimStart().StartsWith("DO ", StringComparison.OrdinalIgnoreCase))
{
    var n = await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"ExecuteNonQuery rows affected: {n}");
}
else
{
    await using var reader = await cmd.ExecuteReaderAsync();
    do
    {
        if (reader.FieldCount == 0)
            continue;

        for (var i = 0; i < reader.FieldCount; i++)
            Console.Write((i > 0 ? "\t" : "") + reader.GetName(i));
        Console.WriteLine();

        while (await reader.ReadAsync())
        {
            for (var i = 0; i < reader.FieldCount; i++)
                Console.Write((i > 0 ? "\t" : "") + reader.GetValue(i));
            Console.WriteLine();
        }
    } while (await reader.NextResultAsync());
}

Console.WriteLine("OK");
return 0;
