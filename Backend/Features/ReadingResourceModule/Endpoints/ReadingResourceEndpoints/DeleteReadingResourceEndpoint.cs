using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using PureTCOWebApp.Core;
using PureTCOWebApp.Core.Models;
using PureTCOWebApp.Data;
using PureTCOWebApp.Features.ReadingResourceModule.Domain;

namespace PureTCOWebApp.Features.ReadingResourceModule.Endpoints.ReadingResourceEndpoints;

public record DeleteReadingResourceRequest(int Id);

public class DeleteReadingResourceEndpoint(
    ApplicationDbContext _dbContext,
    UnitOfWork _unitOfWork
) : Endpoint<DeleteReadingResourceRequest, ApiResponse>
{
    public override void Configure()
    {
        Delete("{id}");
        Group<ReadingResourceEndpointGroup>();
    }

    public override async Task HandleAsync(DeleteReadingResourceRequest req, CancellationToken ct)
    {
        var userId = int.Parse(User.FindFirst("sub")!.Value);
        
        var resource = await _dbContext.Set<ReadingResourceBase>()
            .Include(r => r.ReadingReports)
            .FirstOrDefaultAsync(r => r.Id == req.Id, ct);

        if (resource is null)
        {
            var error = CrudDomainError.NotFound("ReadingResource", req.Id);
            await Send.ResultAsync(TypedResults.BadRequest<ApiResponse>((Result)error));
            return;
        }

        if (resource.UserId != userId)
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        if (!resource.CanBeDeleted())
        {
            var resourceType = resource is Book ? "Book" : "Journal Paper";
            await Send.ResultAsync(TypedResults.BadRequest<ApiResponse>(
                (Result)new Error("DeleteFailed", $"Cannot delete {resourceType} that has reading reports")));
            return;
        }
        
        _dbContext.Remove(resource);
        var result = await _unitOfWork.SaveChangesAsync(ct);

        if (result.IsFailure)
        {
            await Send.ResultAsync(TypedResults.BadRequest<ApiResponse>(result));
            return;
        }

        await Send.OkAsync(Result.Success(), cancellation: ct);
    }
}