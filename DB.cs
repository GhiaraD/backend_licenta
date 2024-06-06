using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Postgres.DB;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {}

    public DbSet<User> Users { get; set; }
    public DbSet<NoiseLevel> NoiseLevels { get; set; }
    public DbSet<UsersInfo> UsersInfos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuring the primary key
        modelBuilder.Entity<NoiseLevel>()
            .HasKey(n => new { n.latitude, n.longitude, n.time });

        // Configuring the table name (if it doesn't match the class name)
        modelBuilder.Entity<NoiseLevel>()
            .ToTable("NoiseLevel");

        modelBuilder.Entity<UsersInfo>().HasNoKey().ToView("UsersInfo");
    }
}

public class DB
{
    public static string GetJWT(WebApplicationBuilder builder, byte[] key, User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
            new Claim("userId", user.userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.username),
            new Claim(JwtRegisteredClaimNames.Email, user.email)
        }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = builder.Configuration["Jwt:Issuer"],
            Audience = builder.Configuration["Jwt:Audience"]
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token).Replace("\n", "").Replace("\r", ""); // Remove newlines and carriage returns
    }


    public static async Task<NoiseLevel> GetLatestNoiseLevel(double latitude, double longitude)
    {
        NoiseLevel result = null;

        var connectionString = "Host=localhost:5432;Username=postgres;Password=password;Database=postgres";
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using (var cmd = dataSource.CreateCommand("SELECT * FROM public.\"NoiseLevel\" WHERE \"latitude\" = @lat AND \"longitude\" = @long ORDER BY \"time\" DESC LIMIT 1"))
        {
            cmd.Parameters.AddWithValue("lat", latitude);
            cmd.Parameters.AddWithValue("long", longitude);
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    result = new NoiseLevel
                    {
                        latitude = reader.GetFieldValue<double>(0),
                        longitude = reader.GetFieldValue<double>(1),
                        time = reader.GetFieldValue<DateTime>(2),
                        LAeq = reader.GetFieldValue<double>(3),
                        LA50 = reader.GetFieldValue<double>(4),
                        measurementsCount = reader.GetFieldValue<int>(5)
                    };
                }
            }
        }

        return result;
    }

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
                NoiseLevel noiseLevel = new NoiseLevel { latitude = reader.GetFieldValue<double>(0), longitude =  reader.GetFieldValue<double>(1), time = reader.GetFieldValue<DateTime>(2), LAeq = reader.GetFieldValue<double>(3), LA50 = reader.GetFieldValue<double>(4), measurementsCount = reader.GetFieldValue<int>(5) };
                result.Add(noiseLevel);
            }
        }
        return result;
    }
}

// generate key fro SHA-256 algorithm
/*using System;
using System.Security.Cryptography;

public class Program
{
    public static void Main()
    {
        // Generate a 256-bit key (32 bytes)
        byte[] key = new byte[32]; // 32 bytes * 8 bits/byte = 256 bits
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(key);
        }

        // Convert the key to a base64 string for easier handling
        string base64Key = Convert.ToBase64String(key);

        Console.WriteLine("Generated Key: " + base64Key);
    }
}*/