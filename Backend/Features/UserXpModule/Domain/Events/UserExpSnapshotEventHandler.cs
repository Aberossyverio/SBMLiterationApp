using Microsoft.EntityFrameworkCore;
using PureTCOWebApp.Core.Events;
using PureTCOWebApp.Data;

namespace PureTCOWebApp.Features.UserXpModule.Domain.Events;

public class UserExpSnapshotEventHandler : IDomainEventHandler<UserExpCreatedEvent>
{
    private readonly ApplicationDbContext _dbContext;

    public UserExpSnapshotEventHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(UserExpCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        var userExpEvent = domainEvent.UserExpEvent;
        
        var snapshot = await _dbContext.UserExpSnapshots
            .FirstOrDefaultAsync(s => s.UserId == userExpEvent.UserId, cancellationToken);
        
        if (snapshot == null)
        {
            snapshot = UserExpSnapshot.Create(
                userExpEvent.UserId,
                1,
                userExpEvent.EventSeq,
                userExpEvent.Exp
            );
            await _dbContext.UserExpSnapshots.AddAsync(snapshot, cancellationToken);
        }
        else
        {
            snapshot.Update(
                userExpEvent.EventSeq,
                snapshot.Exp + userExpEvent.Exp
            );
        }
    }
}
