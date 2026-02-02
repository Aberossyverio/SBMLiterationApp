using Microsoft.EntityFrameworkCore;
using PureTCOWebApp.Core.Events;
using PureTCOWebApp.Data;
using PureTCOWebApp.Features.StreakModule.Domain.Events;

namespace PureTCOWebApp.Features.UserXpModule.Domain.Events;

public class StreakExpEventHandler : IDomainEventHandler<StreakLogCreatedEvent>
{
    private readonly ApplicationDbContext _dbContext;

    public StreakExpEventHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(StreakLogCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        var streakLog = domainEvent.StreakLog;
        
        var last7Days = Enumerable.Range(0, ExpConstants.STREAK_BONUS_DAYS)
            .Select(i => streakLog.StreakDate.AddDays(-i))
            .OrderBy(d => d)
            .ToList();
        
        var streakLogs = await _dbContext.StreakLogs
            .Where(s => s.UserId == streakLog.UserId && last7Days.Contains(s.StreakDate))
            .OrderBy(s => s.StreakDate)
            .ToListAsync(cancellationToken);
        
        if (streakLogs.Count < ExpConstants.STREAK_BONUS_DAYS) return;
        
        var isConsecutive = true;
        for (int i = 1; i < streakLogs.Count; i++)
        {
            if (streakLogs[i].StreakDate != streakLogs[i - 1].StreakDate.AddDays(1))
            {
                isConsecutive = false;
                break;
            }
        }
        
        if (!isConsecutive) return;
        
        var firstDayOfStreak = streakLogs.First().StreakDate;
        
        var alreadyGivenXp = await _dbContext.UserExpEvents
            .AnyAsync(e => e.UserId == streakLog.UserId &&
                          e.EventName == nameof(UserExpEvent.ExpEventType.StreakExp) &&
                          e.RefId == streakLogs.First().Id, cancellationToken);
        
        if (alreadyGivenXp) return;
        
        var maxSeq = await _dbContext.UserExpEvents
            .Where(e => e.UserId == streakLog.UserId)
            .OrderByDescending(e => e.EventSeq)
            .Select(e => e.EventSeq)
            .FirstOrDefaultAsync(cancellationToken);
        
        var nextSeq = maxSeq + 1;
        
        var userExp = UserExpEvent.Create(
            streakLog.UserId,
            nextSeq,
            ExpConstants.STREAK_7_DAYS_BONUS,
            nameof(UserExpEvent.ExpEventType.StreakExp),
            streakLog.Id
        );
        
        userExp.Raise(new UserExpCreatedEvent(userExp));
        
        await _dbContext.UserExpEvents.AddAsync(userExp, cancellationToken);
    }
}
