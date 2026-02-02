using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using PureTCOWebApp.Core;
using PureTCOWebApp.Core.Models;
using PureTCOWebApp.Data;

namespace PureTCOWebApp.Features.StreakModule.Endpoints;

public record DayStreakStatus(
    string Date,
    bool? HasStreak);

public record GetUserStreakResponse(
    int CurrentStreakDays,
    decimal TotalExp,
    List<DayStreakStatus> WeeklyStatus);

public class GetUserStreakEndpoint(ApplicationDbContext context)
    : EndpointWithoutRequest<ApiResponse<GetUserStreakResponse>>
{
    public override void Configure()
    {
        Get("/me");
        Group<StreakEndpointGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirst("sub")!.Value);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(8));

        var daysSinceMonday = ((int)today.DayOfWeek - 1 + 7) % 7;
        var startOfWeek = today.AddDays(-daysSinceMonday);
        var endOfWeek = startOfWeek.AddDays(6);

        var streakLogs = await context.StreakLogs
            .Where(s => s.UserId == userId && 
                       s.StreakDate >= startOfWeek && 
                       s.StreakDate <= endOfWeek)
            .Select(s => s.StreakDate)
            .ToListAsync(ct);

        var weeklyStatus = new List<DayStreakStatus>();
        
        for (int i = 0; i < 7; i++)
        {
            var date = startOfWeek.AddDays(i);
            bool? hasStreak = date > today ? null : streakLogs.Contains(date);
            weeklyStatus.Add(new DayStreakStatus(date.ToString("yyyy-MM-dd"), hasStreak));
        }

        var allStreakLogs = await context.StreakLogs
            .Where(s => s.UserId == userId && s.StreakDate <= today)
            .OrderByDescending(s => s.StreakDate)
            .Select(s => s.StreakDate)
            .ToListAsync(ct);

        int currentStreak = 0;
        if (allStreakLogs.Count != 0)
        {
            var lastStreakDate = allStreakLogs.First();
            
            if (lastStreakDate == today || lastStreakDate == today.AddDays(-1))
            {
                currentStreak = 1;
                var checkDate = lastStreakDate.AddDays(-1);

                foreach (var date in allStreakLogs.Skip(1))
                {
                    if (date == checkDate)
                    {
                        currentStreak++;
                        checkDate = checkDate.AddDays(-1);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        var totalExp = await context.UserExpSnapshots
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SnapshotSeq)
            .Select(s => s.Exp)
            .FirstOrDefaultAsync(ct);

        var response = new GetUserStreakResponse(
            CurrentStreakDays: currentStreak,
            TotalExp: totalExp,
            WeeklyStatus: weeklyStatus);

        await Send.OkAsync(Result.Success(response), ct);
    }
}
