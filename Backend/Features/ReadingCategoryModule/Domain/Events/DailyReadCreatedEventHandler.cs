using Microsoft.EntityFrameworkCore;
using PureTCOWebApp.Core.Events;
using PureTCOWebApp.Data;
using PureTCOWebApp.Features.DailyReadsModule.Domain.Events;

namespace PureTCOWebApp.Features.ReadingCategoryModule.Domain.Events;

public class DailyReadCreatedEventHandler : IDomainEventHandler<DailyReadCreatedEvent>
{
    private readonly ApplicationDbContext _dbContext;

    public DailyReadCreatedEventHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(DailyReadCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(domainEvent.DailyRead.Category))
            return;

        var categoryName = domainEvent.DailyRead.Category.Trim();

        var exists = await _dbContext.ReadingCategories
            .AnyAsync(c => c.CategoryName.ToLower() == categoryName.ToLower(), cancellationToken);

        if (!exists)
        {
            var category = ReadingCategory.Create(categoryName);
            await _dbContext.ReadingCategories.AddAsync(category, cancellationToken);
        }
    }
}
