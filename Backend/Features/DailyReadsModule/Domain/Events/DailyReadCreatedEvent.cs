using PureTCOWebApp.Core.Events;

namespace PureTCOWebApp.Features.DailyReadsModule.Domain.Events;

public record DailyReadCreatedEvent(DailyRead DailyRead) : IDomainEvent;
