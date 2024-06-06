public record NoiseLevel
{
    public required double latitude { get; set; }
    public required double longitude { get; set; }
    public required DateTime time { get; set; }
    public double LAeq { get; set; }
    public double LA50 { get; set; }
    public int measurementsCount { get; set; }
}
public class RegisterDto
{
    public required string username { get; set; }
    public required string password { get; set; }
    public required string email { get; set; }
}

public class LoginDto
{
    public required string username { get; set; }
    public required string password { get; set; }
}

public class User
{
    public int userId { get; set; }
    public required string username { get; set; }
    public string? password { get; set; }
    public required string email { get; set; }
    public int streak { get; set; }
    public int allTimeStreak { get; set; }
    public int score { get; set; }
    public int maxScore { get; set; }
    public String monthMaxScore { get; set; } = "noScore";
    public TimeSpan timeMeasured { get; set; } = TimeSpan.Zero;
    public TimeSpan maxTime { get; set; } = TimeSpan.Zero;
    public String monthMaxTime { get; set; } = "noRecord";
    public TimeSpan allTimeMeasured { get; set; } = TimeSpan.Zero;
}

public class UsersInfo
{
    public int userId { get; set; }
    public DateTime createdAt { get; set; }
    public string username { get; set; }
    public int streak { get; set; }
    public int allTimeStreak { get; set; }
    public int score { get; set; }
    public int maxScore { get; set; }
    public String monthMaxScore { get; set; }
    public TimeSpan timeMeasured { get; set; }
    public TimeSpan maxTime { get; set; }
    public String monthMaxTime { get; set; }
    public TimeSpan allTimeMeasured { get; set; }
}

public class UpdateScoreDto
{
    public int NewScore { get; set; }
}

public class UpdateStreakDto
{
    public int NewStreak { get; set; }
}

public class UpdateTimeMeasuredDto
{
    public TimeSpan NewTimeMeasured { get; set; }
}

public class UpdateAllTimeMeasuredDto
{
    public TimeSpan NewAllTimeMeasured { get; set; }
}