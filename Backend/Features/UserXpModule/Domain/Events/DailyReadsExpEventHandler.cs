using Microsoft.EntityFrameworkCore;
using PureTCOWebApp.Core.Events;
using PureTCOWebApp.Data;
using PureTCOWebApp.Features.DailyReadsModule.Domain.Events;

namespace PureTCOWebApp.Features.UserXpModule.Domain.Events;

public class DailyReadsExpEventHandler : IDomainEventHandler<QuizAnsweredEvent>
{
    private readonly ApplicationDbContext _dbContext;

    public DailyReadsExpEventHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(QuizAnsweredEvent domainEvent, CancellationToken cancellationToken)
    {
        var quizAnswer = domainEvent.QuizAnswer;
        
        var latestAnswers = await _dbContext.QuizAnswers
            .Where(a => a.UserId == quizAnswer.UserId && a.DailyReadId == quizAnswer.DailyReadId)
            .GroupBy(a => a.QuestionSeq)
            .Select(g => g.OrderByDescending(a => a.RetrySeq).First())
            .ToListAsync(cancellationToken);
        
        var questions = await _dbContext.QuizQuestions
            .Where(q => q.DailyReadId == quizAnswer.DailyReadId)
            .ToListAsync(cancellationToken);
        
        var dailyRead = await _dbContext.DailyReads
            .FirstOrDefaultAsync(dr => dr.Id == quizAnswer.DailyReadId, cancellationToken);
        
        if (dailyRead == null) return;
        
        var correctCount = latestAnswers.Count(a =>
            questions.Any(q => q.QuestionSeq == a.QuestionSeq &&
                               q.CorrectAnswer.Equals(a.Answer, StringComparison.OrdinalIgnoreCase)));
        
        if (correctCount < dailyRead.MinimalCorrectAnswer) return;
        
        var alreadyGivenXp = await _dbContext.UserExpEvents
            .AnyAsync(e => e.UserId == quizAnswer.UserId &&
                          e.EventName == nameof(UserExpEvent.ExpEventType.DailyReadsExp) &&
                          e.RefId == quizAnswer.DailyReadId, cancellationToken);
        
        if (alreadyGivenXp) return;
        
        var maxSeq = await _dbContext.UserExpEvents
            .Where(e => e.UserId == quizAnswer.UserId)
            .OrderByDescending(e => e.EventSeq)
            .Select(e => e.EventSeq)
            .FirstOrDefaultAsync(cancellationToken);
        
        var nextSeq = maxSeq + 1;
        
        var userExp = UserExpEvent.Create(
            quizAnswer.UserId,
            nextSeq,
            ExpConstants.DAILY_READ_QUIZ_PASSED,
            nameof(UserExpEvent.ExpEventType.DailyReadsExp),
            quizAnswer.DailyReadId
        );
        
        userExp.Raise(new UserExpCreatedEvent(userExp));
        
        await _dbContext.UserExpEvents.AddAsync(userExp, cancellationToken);
    }
}
