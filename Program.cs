using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Postgres.DB;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Proiect Licenta API", Description = "Sound trek - Bucharest Noise Map with crowdsourcing", Version = "v1" });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Proiect Licenta V1");
    });
}

app.MapPost("/register", async (RegisterDto registerDto, ApplicationDbContext dbContext, IPasswordHasher<User> passwordHasher) =>
{
    if (await dbContext.Users.AnyAsync(u => u.username == registerDto.username))
    {
        return Results.BadRequest("Username already exists.");
    }

    if (await dbContext.Users.AnyAsync(u => u.email == registerDto.email))
    {
        return Results.BadRequest("Email already exists.");
    }

    var user = new User
    {
        username = registerDto.username,
        email = registerDto.email
    };

    user.password = passwordHasher.HashPassword(user, registerDto.password);

    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync();

    string tokenString = DB.GetJWT(builder, key, user);

    return Results.Ok(new { Token = tokenString, UserId = user.userId });
});

app.MapPost("/login", async (LoginDto loginDto, ApplicationDbContext dbContext, IPasswordHasher<User> passwordHasher) =>
{
    var user = await dbContext.Users.SingleOrDefaultAsync(u => u.username == loginDto.username);
    if (user == null || passwordHasher.VerifyHashedPassword(user, user.password, loginDto.password) == PasswordVerificationResult.Failed)
    {
        return Results.Unauthorized();
    }

    string tokenString = DB.GetJWT(builder, key, user);

    return Results.Ok(new { Token = tokenString, UserId = user.userId });
});

app.MapGet("/noiseLevel", async (double latitude, double longitude, ApplicationDbContext dbContext) =>
{
var noiseLevel = await dbContext.NoiseLevels
                                .Where(n => n.latitude == latitude && n.longitude == longitude)
                                .OrderByDescending(n => n.time)
                                .FirstOrDefaultAsync();
    if (noiseLevel == null)
    {
        return Results.NotFound("No noise level data found for the given coordinates.");
    }

    return Results.Ok(noiseLevel);
});

app.MapPost("/noiseLevel", async (NoiseLevel noiseLevel, ApplicationDbContext dbContext) =>
{
    noiseLevel.time = DateTime.SpecifyKind(noiseLevel.time, DateTimeKind.Utc);

    dbContext.NoiseLevels.Add(noiseLevel);
    await dbContext.SaveChangesAsync();

    return Results.Created($"/noiseLevels/{noiseLevel.latitude}/{noiseLevel.longitude}/{noiseLevel.time}", noiseLevel);
});

app.MapGet("/latestMap", async (ApplicationDbContext dbContext) =>
{
    var latestNoiseLevels = await dbContext.NoiseLevels
                                           .GroupBy(n => new { n.latitude, n.longitude })
                                           .Select(g => g.OrderByDescending(n => n.time).First())
                                           .ToListAsync();

    return Results.Ok(latestNoiseLevels);
});

app.MapGet("/noiseLevelsByDay", async (double latitude, double longitude, DateTime day, ApplicationDbContext dbContext) =>
{
    var startDate = DateTime.SpecifyKind(day.Date, DateTimeKind.Utc);
    var endDate = startDate.AddDays(1);

    var noiseLevels = await dbContext.NoiseLevels
                                     .Where(n => n.latitude == latitude && n.longitude == longitude && n.time >= startDate && n.time < endDate)
                                     .ToListAsync();

    if (noiseLevels.Count == 0)
    {
        return Results.NotFound("No noise level data found for the given coordinates and day.");
    }

    return Results.Ok(noiseLevels);
});

app.MapGet("/noiseLevelsByMonth", async (double latitude, double longitude, DateTime day, ApplicationDbContext dbContext) =>
{
    var startDate = new DateTime(day.Year, day.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    var endDate = startDate.AddMonths(1);

    var noiseLevels = await dbContext.NoiseLevels
                                     .Where(n => n.latitude == latitude && n.longitude == longitude && n.time >= startDate && n.time < endDate)
                                     .ToListAsync();

    if (noiseLevels.Count == 0)
    {
        return Results.NotFound("No noise level data found for the given coordinates and month.");
    }

    return Results.Ok(noiseLevels);
});

app.MapGet("/noiseLevelsByYear", async (double latitude, double longitude, DateTime day, ApplicationDbContext dbContext) =>
{
    var startDate = new DateTime(day.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    var endDate = startDate.AddYears(1);

    var noiseLevels = await dbContext.NoiseLevels
                                     .Where(n => n.latitude == latitude && n.longitude == longitude && n.time >= startDate && n.time < endDate)
                                     .ToListAsync();

    if (noiseLevels.Count == 0)
    {
        return Results.NotFound("No noise level data found for the given coordinates and year.");
    }

    return Results.Ok(noiseLevels);
});

app.MapGet("/noiseLevelsByWeek", async (double latitude, double longitude, DateTime day, ApplicationDbContext dbContext) =>
{
    // Calculate the start and end of the week (Monday to Sunday)
    var dayOfWeek = (int)day.DayOfWeek;
    var startOfWeek = DateTime.SpecifyKind(day.Date.AddDays(-(dayOfWeek == 0 ? 6 : dayOfWeek - 1)), DateTimeKind.Utc);
    var endOfWeek = DateTime.SpecifyKind(startOfWeek.AddDays(7), DateTimeKind.Utc);

    var noiseLevels = await dbContext.NoiseLevels
                                     .Where(n => n.latitude == latitude && n.longitude == longitude && n.time >= startOfWeek && n.time < endOfWeek)
                                     .ToListAsync();

    if (noiseLevels.Count == 0)
    {
        return Results.NotFound("No noise level data found for the given coordinates and week.");
    }

    return Results.Ok(noiseLevels);
});

app.MapGet("/userInfo/{userId}", async (int userId, ApplicationDbContext dbContext) =>
{
    var userInfo = await dbContext.Set<UsersInfo>()
                                  .FromSqlRaw("SELECT * FROM public.\"UsersInfo\" WHERE \"userId\" = {0}", userId)
                                  .FirstOrDefaultAsync();

    if (userInfo == null)
    {
        return Results.NotFound("User not found.");
    }

    return Results.Ok(userInfo);
});

app.MapGet("/userInfo/score", async (ApplicationDbContext dbContext) =>
{
    var topUsers = await dbContext.Set<UsersInfo>()
                                  .FromSqlRaw("SELECT * FROM public.\"UsersInfo\" ORDER BY \"score\" DESC LIMIT 100")
                                  .ToListAsync();

    if (topUsers == null || topUsers.Count == 0)
    {
        return Results.NotFound("No users found.");
    }

    return Results.Ok(topUsers);
});

app.MapGet("/userInfo/maxScore", async (ApplicationDbContext dbContext) =>
{
    var topUsers = await dbContext.Set<UsersInfo>()
                                  .FromSqlRaw("SELECT * FROM public.\"UsersInfo\" ORDER BY \"maxScore\" DESC LIMIT 100")
                                  .ToListAsync();

    if (topUsers == null || topUsers.Count == 0)
    {
        return Results.NotFound("No users found.");
    }

    return Results.Ok(topUsers);
});

app.MapGet("/userInfo/streak", async (ApplicationDbContext dbContext) =>
{
    var topUsers = await dbContext.Set<UsersInfo>()
                                  .FromSqlRaw("SELECT * FROM public.\"UsersInfo\" ORDER BY \"streak\" DESC LIMIT 100")
                                  .ToListAsync();

    if (topUsers == null || topUsers.Count == 0)
    {
        return Results.NotFound("No users found.");
    }

    return Results.Ok(topUsers);
});

app.MapGet("/userInfo/allTimeStreak", async (ApplicationDbContext dbContext) =>
{
    var topUsers = await dbContext.Set<UsersInfo>()
                                  .FromSqlRaw("SELECT * FROM public.\"UsersInfo\" ORDER BY \"allTimeStreak\" DESC LIMIT 100")
                                  .ToListAsync();

    if (topUsers == null || topUsers.Count == 0)
    {
        return Results.NotFound("No users found.");
    }

    return Results.Ok(topUsers);
});

app.MapPut("/userInfo/{userId}/score", async (int userId, ApplicationDbContext dbContext, [FromBody] UpdateScoreDto dto) =>
{
    var user = await dbContext.Users.FindAsync(userId);
    if (user == null)
    {
        return Results.NotFound("User not found.");
    }

    user.score = user.score + dto.NewScore;
    if (user.score > user.maxScore)
    {
        user.maxScore = user.score;
        user.monthMaxScore = DateTime.UtcNow.ToString("MMMM-yyyy");
    }
    await dbContext.SaveChangesAsync();

    return Results.Ok(new { userId = user.userId, score = user.score });
});

app.MapPut("/userInfo/{userId}/streak", async (int userId, ApplicationDbContext dbContext, [FromBody] UpdateStreakDto dto) =>
{
    var user = await dbContext.Users.FindAsync(userId);
    if (user == null)
    {
        return Results.NotFound("User not found.");
    }

    user.streak = dto.NewStreak;
    if (dto.NewStreak > user.allTimeStreak)
    {
        user.allTimeStreak = dto.NewStreak;
    }
    await dbContext.SaveChangesAsync();

    return Results.Ok(new { userId = user.userId, streak = user.streak });
});

app.MapPut("/userInfo/{userId}/timeMeasured", async (int userId, ApplicationDbContext dbContext, [FromBody] UpdateTimeMeasuredDto dto) =>
{
    var user = await dbContext.Users.FindAsync(userId);
    if (user == null)
    {
        return Results.NotFound("User not found.");
    }

    user.timeMeasured = dto.NewTimeMeasured;
    if (dto.NewTimeMeasured > user.maxTime)
    {
        user.maxTime = dto.NewTimeMeasured;
        user.monthMaxTime = DateTime.UtcNow.ToString("MMMM-yyyy");
    }
    await dbContext.SaveChangesAsync();

    return Results.Ok(new { userId = user.userId, timeMeasured = user.timeMeasured });
});

app.MapPut("/userInfo/{userId}/allTimeMeasured", async (int userId, ApplicationDbContext dbContext, [FromBody] UpdateAllTimeMeasuredDto dto) =>
{
    var user = await dbContext.Users.FindAsync(userId);
    if (user == null)
    {
        return Results.NotFound("User not found.");
    }

    user.allTimeMeasured = dto.NewAllTimeMeasured;
    await dbContext.SaveChangesAsync();

    return Results.Ok(new { userId = user.userId, timeMeasured = user.allTimeMeasured });
});

app.Run();