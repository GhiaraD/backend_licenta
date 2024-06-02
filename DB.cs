using Npgsql;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Postgres.DB;

public record NoiseLevel
{

    public required double Lat { get; set; }
    public required double Long { get; set; }
    public required DateTime Time { get; set; }
    public double LAeq { get; set; }
    public double LA50 { get; set; }
    public int MeasurementsCount { get; set; }
}

public class DB
{

    public static async Task<List<NoiseLevel>> GetAllNoiseLevels()
    {
        List<NoiseLevel> result = [];
 
        var connectionString = "Host=localhost:5432;Username=postgres;Password=password;Database=postgres";
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using (var cmd = dataSource.CreateCommand("SELECT * FROM public.\"NoiseLevel\""))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                NoiseLevel noiseLevel = new NoiseLevel { Lat = reader.GetFieldValue<double>(0), Long =  reader.GetFieldValue<double>(1), Time = reader.GetFieldValue<DateTime>(2), LAeq = reader.GetFieldValue<double>(3), LA50 = reader.GetFieldValue<double>(4), MeasurementsCount = reader.GetFieldValue<int>(5) };
                result.Add(noiseLevel);
            }
        }
        return result;
    }
}